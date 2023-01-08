# verify-node-modules

Perform a cursory check of `node_modules` to verify everything specified in `package-lock.json` is present.

Initial version is written in C# for .NET Framework/Core.

## :warning: Outdated

This project was built quite some time ago and not really updated since. In the meantime, the `package-lock.json` format has evolved. This means the program(s) may either crash or, worse, produce false positives/negatives. **The programs in this repository should no longer be used as-is.**

Futhermore, changes to `npm` make `npm install` a sane command again. On the other hand, `npm ci` is not sane at all and should IMHO not be used at all.
