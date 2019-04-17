# verify-node-modules

Perform a cursory check of `node_modules` to verify everything specified in `package-lock.json` is present.

Initial version is written in C# for .NET Framework/Core.

## Motivation

Doing `npm install` or `npm ci` on every build is not feasible. First, we should check if it’s necessary.
