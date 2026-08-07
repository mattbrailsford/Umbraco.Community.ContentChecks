import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { umbPeekError } from "@umbraco-cms/backoffice/notification";
import type { UmbDetailRepository } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

export interface ContentChecksDeleteActionArgs {
    headline: string;
    confirmMessage: string;
    getRepository: (host: UmbControllerHost) => UmbDetailRepository<unknown>;
    /** Path to navigate to after successful deletion. If not provided, navigates to the section root. */
    navigateTo?: string;
}

/**
 * Reusable delete-action base -- confirm, call the repository, surface a server error, or
 * navigate away on success. Not the built-in `kind: "delete"` entity action, which additionally
 * requires an item-repository this project hasn't built.
 */
export abstract class ContentChecksDeleteActionBase extends UmbEntityActionBase<never> {
    protected abstract getArgs(): ContentChecksDeleteActionArgs;

    async execute() {
        if (!this.args.unique) {
            throw new Error("Cannot delete without unique identifier.");
        }

        const { headline, confirmMessage, getRepository, navigateTo } = this.getArgs();

        await umbConfirmModal(this, {
            headline,
            content: confirmMessage,
            color: "danger",
            confirmLabel: "#actions_delete",
        });

        const repository = getRepository(this);
        const { error } = await repository.delete(this.args.unique);

        if (error) {
            const apiError = error as { title?: string; detail?: string; problemDetails?: { title?: string; detail?: string } };
            const title = apiError.problemDetails?.title ?? apiError.title;
            const detail = apiError.problemDetails?.detail ?? apiError.detail;
            await umbPeekError(this, {
                headline: title ?? "Error",
                message: detail ?? "The item could not be deleted.",
            });
            throw error;
        }

        history.pushState(null, "", navigateTo ?? "/umbraco/section/content-checks");
    }
}
