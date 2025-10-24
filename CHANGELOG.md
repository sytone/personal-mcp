Personal MCP Change Log
<a name="0.3.0"></a>
## [0.3.0](https://www.github.com/sytone/personal-mcp/releases/tag/v0.3.0) (2025-10-23)

### ✨ Features

* Add Liquid template rendering support with ITemplateService and TemplateService ([75d034a](https://www.github.com/sytone/personal-mcp/commit/75d034ace74b06e1334d885dec451bd63c83033b))
* **database:** add flexible SQLite connection configuration ([bb37149](https://www.github.com/sytone/personal-mcp/commit/bb37149558286e73b51d103c77f1245caf280fd4))
* **date-tools:** add utilities for relative date calculations ([013a7e6](https://www.github.com/sytone/personal-mcp/commit/013a7e6ccd496eff4e32acc8bc9f4de611d13f34))
* **journal:** add AddJournalTask method with unit tests ([7e691bc](https://www.github.com/sytone/personal-mcp/commit/7e691bc1d8e92c7c21eb9991623977cc008a4c06))
* **journal:** enhance journal entry management with new methods ([3979069](https://www.github.com/sytone/personal-mcp/commit/3979069b07d263267bc76fcd52744e70a8e35f90))
* **journal:** update journal template to use monday_iso_week ([c98b3b5](https://www.github.com/sytone/personal-mcp/commit/c98b3b50e260e29b756c660010113fa1e6b0f4d4))
* **list-notes:** add date filtering by creation or modification ([162f01b](https://www.github.com/sytone/personal-mcp/commit/162f01bba0fa7f2f54b4170f7af3b8f8ab0b9ff6))
* **search:** implement in-memory search index and remove SQLite dependency ([7f91553](https://www.github.com/sytone/personal-mcp/commit/7f9155377b46cc749d40fea97fdbdd893b141b96))
* **settings:** implement vault-level settings from markdown ([2e504fa](https://www.github.com/sytone/personal-mcp/commit/2e504fa5e15cc1fb1e9090004be7ade084e3f618))

### 🐛 Bug Fixes

* updated nuget package metadata ([69fe4e3](https://www.github.com/sytone/personal-mcp/commit/69fe4e38ea95790257c9bbe4f2a3feb8122f4bd0))
* **journal:** improve error handling for invalid journal paths ([5369724](https://www.github.com/sytone/personal-mcp/commit/536972427465efdf2e54c3d72e3a6adc72dd28cf))
* **journal:** improve error handling for non-existent journal paths ([65c0665](https://www.github.com/sytone/personal-mcp/commit/65c06650b0e002a32d669c21e8e571eac316ab1b))

### Other

* Add MIT License to the project ([938febf](https://www.github.com/sytone/personal-mcp/commit/938febf8c9f58328a4a922a06a1ce2b9fcac34f1))
* first commit ([906a812](https://www.github.com/sytone/personal-mcp/commit/906a812ce926a76f43fcb927656e2ed51c91f7cd))
* Merge branches 'main' and 'main' of https://github.com/sytone/personal-mcp ([a20ac4a](https://www.github.com/sytone/personal-mcp/commit/a20ac4a383c5d78ab7392362c3ba8d46313a4059))
* :construction_worker: updated release version and added inital publish script ([6378d8b](https://www.github.com/sytone/personal-mcp/commit/6378d8b2c709362f5cfe77db7ee097b6a610671b))
* add test project and implement comprehensive unit tests for JournalTools ([54a9fec](https://www.github.com/sytone/personal-mcp/commit/54a9fec51107470b9d3054a7d26732af08063038))
* add versionize support to publish script ([22f2932](https://www.github.com/sytone/personal-mcp/commit/22f2932835ea6d7861c83c1a78a797f0b411f882))
* bump version to 0.2.1 ([945c5ad](https://www.github.com/sytone/personal-mcp/commit/945c5ad43ff4eb0f9b030c92a8370a6453d4d33e))
* enable package validation in project file ([66ef3b6](https://www.github.com/sytone/personal-mcp/commit/66ef3b6fdcdf58b5b5f02a16c7431305dfbffa19))
* inital public release ([87165d7](https://www.github.com/sytone/personal-mcp/commit/87165d736e33ad92084cb4496d3d7d7505ff2786))
* Refactor vault service usage in tools to use IVaultService interface ([356501c](https://www.github.com/sytone/personal-mcp/commit/356501c25bf286225232c772d405473103e0b97e))
* streamline TestVaultFixture initialization and file population logic ([16f6b1b](https://www.github.com/sytone/personal-mcp/commit/16f6b1bbfd6213bcf4c7aae3d016c0665aa5aa9b))
* update publish script to enhance versioning and build process ([ffeac65](https://www.github.com/sytone/personal-mcp/commit/ffeac6598dc6424a93ab0d20ebe732208761bb81))
* update upload and download artifact actions to v4 ([bf72351](https://www.github.com/sytone/personal-mcp/commit/bf723510a74384ae2312d7d6a02a529671c3f489))
* **journal:** add assertion for journalTasksHeading setting ([aff988b](https://www.github.com/sytone/personal-mcp/commit/aff988b4cb7981778c34218c450bba7352b7c4e7))
* **journal:** add tests for sequential journal entry insertion ([8fff175](https://www.github.com/sytone/personal-mcp/commit/8fff17534132a3c40eea9953097937b837b649ea))
* **release:** 0.2.1 ([90f8a9d](https://www.github.com/sytone/personal-mcp/commit/90f8a9d2e7ebb0008c6ef779a1324adcbf264133))
* **scripts:** update nuget package output directory ([7b15f1c](https://www.github.com/sytone/personal-mcp/commit/7b15f1c60d841c0b679087bb922e14c969730e12))
* **tests:** improve directory creation in journal tests ([a7c2b0c](https://www.github.com/sytone/personal-mcp/commit/a7c2b0c3ee4d96a4467619eeb00f4e6b8631266b))
* **tests:** use platform-agnostic paths in TestVaultFixture ([2e8445c](https://www.github.com/sytone/personal-mcp/commit/2e8445cd9e03243941dc0a90eceba7ca921d7bb0))
* **tools:** refactor file operations to use IFileSystem abstraction ([c442c87](https://www.github.com/sytone/personal-mcp/commit/c442c8785345f3c21dfe0c80a0d33f1b74472d24))

<a name="0.2.1"></a>
## [0.2.1](https://www.github.com/sytone/personal-mcp/releases/tag/v0.2.1) (2025-10-19)

### 🐛 Bug Fixes

* updated nuget package metadata ([69fe4e3](https://www.github.com/sytone/personal-mcp/commit/69fe4e38ea95790257c9bbe4f2a3feb8122f4bd0))

### Other

* Add MIT License to the project ([938febf](https://www.github.com/sytone/personal-mcp/commit/938febf8c9f58328a4a922a06a1ce2b9fcac34f1))
* first commit ([906a812](https://www.github.com/sytone/personal-mcp/commit/906a812ce926a76f43fcb927656e2ed51c91f7cd))
* Merge branches 'main' and 'main' of https://github.com/sytone/personal-mcp ([a20ac4a](https://www.github.com/sytone/personal-mcp/commit/a20ac4a383c5d78ab7392362c3ba8d46313a4059))
* :construction_worker: updated release version and added inital publish script ([6378d8b](https://www.github.com/sytone/personal-mcp/commit/6378d8b2c709362f5cfe77db7ee097b6a610671b))
* add versionize support to publish script ([22f2932](https://www.github.com/sytone/personal-mcp/commit/22f2932835ea6d7861c83c1a78a797f0b411f882))
* enable package validation in project file ([66ef3b6](https://www.github.com/sytone/personal-mcp/commit/66ef3b6fdcdf58b5b5f02a16c7431305dfbffa19))
* inital public release ([87165d7](https://www.github.com/sytone/personal-mcp/commit/87165d736e33ad92084cb4496d3d7d7505ff2786))
* update publish script to enhance versioning and build process ([ffeac65](https://www.github.com/sytone/personal-mcp/commit/ffeac6598dc6424a93ab0d20ebe732208761bb81))
* update upload and download artifact actions to v4 ([bf72351](https://www.github.com/sytone/personal-mcp/commit/bf723510a74384ae2312d7d6a02a529671c3f489))
* **release:** 0.2.1 ([90f8a9d](https://www.github.com/sytone/personal-mcp/commit/90f8a9d2e7ebb0008c6ef779a1324adcbf264133))

