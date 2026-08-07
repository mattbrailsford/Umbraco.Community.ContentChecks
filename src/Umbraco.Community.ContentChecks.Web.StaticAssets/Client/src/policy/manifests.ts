import { policyTreeManifests } from "./tree/manifests.js";
import { policyFolderManifests } from "./folder/manifests.js";
import { policyRepositoryManifests } from "./repository/manifests.js";
import { policyEntityActionManifests } from "./entity-actions/manifests.js";
import { policyWorkspaceManifests } from "./workspace/policy/manifests.js";
import { policyRuleModalManifests } from "./modals/manifests.js";

export const policyManifests: Array<UmbExtensionManifest> = [
    ...policyTreeManifests,
    ...policyFolderManifests,
    ...policyRepositoryManifests,
    ...policyEntityActionManifests,
    ...policyWorkspaceManifests,
    ...policyRuleModalManifests,
];
