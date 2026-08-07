import type { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { client } from "./api/client.gen.js";
import { configureContentChecksClient } from "./core/client/index.js";

export * from "./index.js";
export * from "./exports.js";

export const onInit: UmbEntryPointOnInit = (host, _extensionRegistry) => {
    configureContentChecksClient(host, client);
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {};
