## [0.3.0] - 2025-10-24

### 🚀 Features

- *(date-tools)* Add utilities for relative date calculations
- *(database)* Add flexible SQLite connection configuration
- *(list-notes)* Add date filtering by creation or modification
- *(search)* Implement in-memory search index and remove SQLite dependency
- *(settings)* Implement vault-level settings from markdown
- *(journal)* Add AddJournalTask method with unit tests
- *(journal)* Enhance journal entry management with new methods
- Add Liquid template rendering support with ITemplateService and TemplateService
- *(journal)* Update journal template to use monday_iso_week

### 🐛 Bug Fixes

- *(journal)* Improve error handling for invalid journal paths
- *(journal)* Improve error handling for non-existent journal paths

### 🚜 Refactor

- Refactor vault service usage in tools to use IVaultService interface
- Streamline TestVaultFixture initialization and file population logic
- *(tools)* Refactor file operations to use IFileSystem abstraction
- *(tests)* Use platform-agnostic paths in TestVaultFixture

### 🧪 Testing

- Add test project and implement comprehensive unit tests for JournalTools
- *(journal)* Add tests for sequential journal entry insertion
- *(tests)* Improve directory creation in journal tests
- *(journal)* Add assertion for journalTasksHeading setting

### ⚙️ Miscellaneous Tasks

- *(scripts)* Update nuget package output directory
- Bump version to 0.3.0

## [0.2.1] - 2025-10-19

### 💼 Other

- :construction_worker: updated release version and added inital publish script
- Update publish script to enhance versioning and build process
- Add versionize support to publish script

### ⚙️ Miscellaneous Tasks

- *(release)* 0.2.1
- Bump version to 0.2.1

## [0.2.0] - 2025-10-18

### 🐛 Bug Fixes

- Updated nuget package metadata

### 💼 Other

- Inital public release

### ⚙️ Miscellaneous Tasks

- Update upload and download artifact actions to v4
- Enable package validation in project file

