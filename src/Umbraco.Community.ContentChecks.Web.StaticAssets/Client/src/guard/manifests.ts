import { guardTreeManifests } from "./tree/manifests.js";
import { guardRepositoryManifests } from "./repository/manifests.js";
import { guardEntityActionManifests } from "./entity-actions/manifests.js";
import { guardWorkspaceManifests } from "./workspace/guard/manifests.js";
import { guardRuleModalManifests } from "./modals/manifests.js";

export const guardManifests: Array<UmbExtensionManifest> = [
    ...guardTreeManifests,
    ...guardRepositoryManifests,
    ...guardEntityActionManifests,
    ...guardWorkspaceManifests,
    ...guardRuleModalManifests,
];
