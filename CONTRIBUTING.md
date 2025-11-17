# Contributing to DraftPuck

Thank you for your interest in contributing!

## Branch Strategy
- **main** → production
- **develop** → integration branch (PR target)
- **feature/*** → contributor feature branches

All pull requests must target **develop**.

## Fork & Clone Workflow
1. Fork the repository: https://github.com/tyler-roper/DraftPuck
2. Clone your fork:
```bash
git clone https://github.com/<your-username>/DraftPuck.git
cd DraftPuck
```
3. Add upstream remote:
```bash
git remote add upstream https://github.com/tyler-roper/DraftPuck.git
```

## Create a Feature Branch
```bash
git checkout develop
git pull upstream develop
git checkout -b feature/my-feature
```

## Commit & Push Changes
```bash
git add .
git commit -m "Add feature description"
git push origin feature/my-feature
```

## Open a Pull Request
- Base branch: `develop`
- Compare branch: your feature branch

## Code Style & Testing
- Use C# primary constructors
- Follow `.editorconfig`
- Run tests locally before PR