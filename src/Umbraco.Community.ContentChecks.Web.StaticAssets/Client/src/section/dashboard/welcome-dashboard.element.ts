import { css, customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { CONTENT_CHECKS_ROOT_PARENT_UNIQUE } from "../../core/constants.js";
import { CONTENT_CHECKS_CREATE_POLICY_WORKSPACE_PATH_PATTERN } from "../../policy/workspace/policy/paths.js";

/**
 * The section's default view — a short "what this does" intro plus one primary button pointing
 * at the first real action a new install needs, the same shape as Umbraco.AI's own section
 * dashboard. Deliberately not a richer view showing site-wide check status: that needs a
 * branch/site-wide sweep this project doesn't have yet.
 */
@customElement("cc-welcome-dashboard")
export class CcWelcomeDashboardElement extends UmbLitElement {
    override render() {
        const createPolicyPath = CONTENT_CHECKS_CREATE_POLICY_WORKSPACE_PATH_PATTERN.generateAbsolute({
            parentUnique: CONTENT_CHECKS_ROOT_PARENT_UNIQUE,
        });

        return html`
            <div class="uui-text">
                <div class="container">
                    <h1 class="uui-h2" style="margin-top: 0;">Welcome to Content Checks</h1>
                    <p class="intro">
                        Turn "what good content looks like" into checks editors and AI agents can both run --
                        a repeatable quality bar, not a one-off review.
                    </p>
                    <p>
                        Use the menu on the left to browse existing policies, or create your first one to attach
                        a rule to a content type.
                    </p>
                    <uui-button look="primary" color="positive" label="Create a Policy" href=${createPolicyPath}>
                        Create a Policy
                    </uui-button>
                </div>
            </div>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                box-sizing: border-box;
                padding: var(--uui-size-layout-3);
                height: 100%;
                overflow-y: auto;
            }
            .container {
                max-width: 600px;
                margin: 0 auto;
                text-align: center;
            }
            .uui-text .intro {
                font-size: var(--uui-size-6);
                color: var(--uui-color-text-secondary);
                font-weight: 300;
                margin-bottom: var(--uui-size-layout-2);
            }
            uui-button {
                margin-top: var(--uui-size-space-5);
            }
        `,
    ];
}

export default CcWelcomeDashboardElement;

declare global {
    interface HTMLElementTagNameMap {
        "cc-welcome-dashboard": CcWelcomeDashboardElement;
    }
}
