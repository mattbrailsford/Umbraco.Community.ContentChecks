import { css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UmbPropertyValueData, UmbPropertyDatasetElement } from "@umbraco-cms/backoffice/property";
import type { CheckConfigFieldResponseModel, CheckConfigSchemaResponseModel } from "../../../api/types.gen.js";

/**
 * Event detail for model editor value changes.
 */
export interface ContentChecksModelEditorChangeEventDetail {
    model: Record<string, unknown>;
}

/**
 * Reusable model editor component that renders a dynamic form based on a check's config schema.
 * Uses Umbraco's own property dataset and property elements for rendering fields -- no bespoke
 * form per check type.
 *
 * @fires change - Fired when field values change
 */
@customElement("cc-model-editor")
export class CcModelEditorElement extends UmbLitElement {
    @property({ type: Object })
    schema?: CheckConfigSchemaResponseModel;

    @property({ type: Object })
    model?: Record<string, unknown>;

    @property({ type: String, attribute: "empty-message" })
    emptyMessage?: string;

    @state()
    private _propertyValues: UmbPropertyValueData[] = [];

    #isInitialized = false;
    #lastEmittedModel: Record<string, unknown> | null = null;

    override shouldUpdate(changedProperties: Map<string, unknown>): boolean {
        if (this.#isInitialized && changedProperties.size === 1 && changedProperties.has("model")) {
            if (this.#isModelEchoUpdate(this.model)) {
                return false;
            }
            this.#isInitialized = false;
        }
        return true;
    }

    override updated(changedProperties: Map<string, unknown>) {
        if (changedProperties.has("schema")) {
            this.#isInitialized = false;
            this.#lastEmittedModel = null;
        }

        if (!this.#isInitialized && this.schema) {
            this.#populatePropertyValues();
            this.#isInitialized = true;
        }
    }

    #populatePropertyValues() {
        if (!this.schema) {
            this._propertyValues = [];
            return;
        }
        this._propertyValues = this.schema.fields.map((field) => ({
            alias: field.key,
            value: this.model?.[field.key] ?? field.defaultValue,
        }));

        const populatedModel = this._propertyValues.reduce(
            (acc, curr) => ({ ...acc, [curr.alias]: curr.value }),
            {} as Record<string, unknown>,
        );
        if (!this.#isModelEqual(this.model, populatedModel)) {
            this.#lastEmittedModel = populatedModel;
            this.updateComplete.then(() => {
                this.dispatchEvent(
                    new CustomEvent<ContentChecksModelEditorChangeEventDetail>("change", {
                        detail: { model: populatedModel },
                        bubbles: true,
                        composed: true,
                    }),
                );
            });
        }
    }

    #isModelEchoUpdate(incomingModel: Record<string, unknown> | undefined): boolean {
        if (!this.#lastEmittedModel || !incomingModel) {
            return false;
        }
        return this.#isModelEqual(incomingModel, this.#lastEmittedModel);
    }

    #isModelEqual(a: Record<string, unknown> | undefined, b: Record<string, unknown>): boolean {
        if (!a) return false;
        const aKeys = Object.keys(a);
        const bKeys = Object.keys(b);
        if (aKeys.length !== bKeys.length) return false;
        return bKeys.every((key) => a[key] === b[key]);
    }

    #onChange(e: Event) {
        const dataset = e.target as UmbPropertyDatasetElement;
        const model = dataset.value.reduce(
            (acc, curr) => ({ ...acc, [curr.alias]: curr.value }),
            {} as Record<string, unknown>,
        );

        this.#lastEmittedModel = model;

        this.dispatchEvent(
            new CustomEvent<ContentChecksModelEditorChangeEventDetail>("change", {
                detail: { model },
                bubbles: true,
                composed: true,
            }),
        );
    }

    #propertyConfigCache = new Map<string, { source: unknown; value: Array<{ alias: string; value: unknown }> }>();

    #toPropertyConfig(field: CheckConfigFieldResponseModel): Array<{ alias: string; value: unknown }> {
        const cached = this.#propertyConfigCache.get(field.key);
        if (cached && cached.source === field.editorConfig) return cached.value;

        const value = this.#convertPropertyConfig(field.editorConfig);
        this.#propertyConfigCache.set(field.key, { source: field.editorConfig, value });

        return value;
    }

    #convertPropertyConfig(config: unknown): Array<{ alias: string; value: unknown }> {
        if (!config) return [];
        if (Array.isArray(config)) return config as Array<{ alias: string; value: unknown }>;
        if (typeof config !== "object") return [];
        return Object.entries(config).map(([alias, value]) => ({ alias, value }));
    }

    #groupFields(fields: CheckConfigFieldResponseModel[]): Array<[string, CheckConfigFieldResponseModel[]]> {
        const generalKey = "General";
        const groups = new Map<string, CheckConfigFieldResponseModel[]>();
        groups.set(generalKey, []);

        for (const field of fields) {
            const key = field.group ?? generalKey;
            if (!groups.has(key)) {
                groups.set(key, []);
            }
            groups.get(key)!.push(field);
        }

        if (groups.get(generalKey)!.length === 0) {
            groups.delete(generalKey);
        }

        for (const groupFields of groups.values()) {
            // sortOrder is typed number | string by the generated client (the API's own int
            // schema is deliberately lenient about numeric-string input) -- coerce for comparison.
            groupFields.sort((a, b) => Number(a.sortOrder) - Number(b.sortOrder));
        }

        return Array.from(groups.entries());
    }

    #renderField(field: CheckConfigFieldResponseModel) {
        return html`
            <umb-property
                label=${field.label}
                description=${field.description ?? ""}
                alias=${field.key}
                property-editor-ui-alias=${field.editorUiAlias ?? "Umb.PropertyEditorUi.TextBox"}
                .config=${this.#toPropertyConfig(field)}
                .validation=${{ mandatory: field.isRequired }}
            >
            </umb-property>
        `;
    }

    override render() {
        if (!this.schema) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (this.schema.fields.length === 0) {
            return html`<p class="placeholder-text">${this.emptyMessage ?? "No configurable fields."}</p>`;
        }

        return html`
            <umb-property-dataset .value=${this._propertyValues} @change=${this.#onChange}>
                ${this.#groupFields(this.schema.fields).map(
                    ([groupKey, fields]) =>
                        html`<uui-box headline=${groupKey}>${fields.map((f) => this.#renderField(f))}</uui-box>`,
                )}
            </umb-property-dataset>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }
            .placeholder-text {
                margin: 0;
                padding: var(--uui-size-space-5) 0;
                color: var(--uui-color-text-alt);
                font-style: italic;
            }
            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
                margin-top: var(--uui-size-layout-1);
            }
            uui-box:first-child {
                margin-top: 0;
            }
        `,
    ];
}

export default CcModelEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "cc-model-editor": CcModelEditorElement;
    }
}
