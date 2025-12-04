# Publishing ZIMA to npm Registry

## 📦 Publishing Steps

### 1. Create npm Account (if needed)
```bash
# Sign up at: https://www.npmjs.com/signup
# Or use existing account
```

### 2. Login to npm
```bash
npm login
```

Enter your:
- Username
- Password
- Email
- 2FA code (if enabled)

### 3. Verify package.json

Check these fields are correct:
```json
{
  "name": "@zima-ai/zima",           // Scoped package name
  "version": "3.0.0",                // Semantic versioning
  "description": "...",              // Clear description
  "author": "ZIMA Team",             // Your name/org
  "license": "MIT",                  // Open source license
  "repository": {                    // GitHub repo URL
    "type": "git",
    "url": "https://github.com/yourusername/zima.git"
  }
}
```

### 4. Update Repository URL

Before publishing, update in `package.json`:
```json
"repository": {
  "type": "git",
  "url": "https://github.com/Andrew-Mashamba/zima_code.git"
}
```

### 5. Test Package Locally

```bash
# Create a test directory
mkdir /tmp/zima-test
cd /tmp/zima-test

# Install ZIMA from local directory
npm install -g /Volumes/DATA/QWEN

# Test it works
zima
```

### 6. Publish to npm

```bash
cd /Volumes/DATA/QWEN

# Dry run (see what will be published)
npm publish --dry-run

# Publish for real (first time: public access required for scoped packages)
npm publish --access public
```

### 7. Verify Publication

```bash
# Check on npm registry
open https://www.npmjs.com/package/@zima-ai/zima

# Test installation from npm
npm install -g @zima-ai/zima
```

---

## 🔄 Updating Published Package

### Update Version

Follow semantic versioning:
- **Patch** (3.0.0 → 3.0.1): Bug fixes
  ```bash
  npm version patch
  ```

- **Minor** (3.0.0 → 3.1.0): New features (backward compatible)
  ```bash
  npm version minor
  ```

- **Major** (3.0.0 → 4.0.0): Breaking changes
  ```bash
  npm version major
  ```

### Publish Update

```bash
npm publish
```

---

## 📝 Pre-Publish Checklist

- [ ] **README.md** is clear and complete
- [ ] **package.json** has correct:
  - [ ] name (unique on npm)
  - [ ] version (follows semver)
  - [ ] description
  - [ ] keywords (for searchability)
  - [ ] repository URL
  - [ ] author
  - [ ] license
- [ ] **postinstall.js** works correctly
- [ ] **.npmignore** excludes dev files
- [ ] **Dependencies** are production-only (no devDependencies in dependencies)
- [ ] **Tested locally** with `npm link`
- [ ] **Documentation** files included (ZIMA_GUIDE.md, TOOLS_REFERENCE.md)
- [ ] **License file** exists (MIT)
- [ ] **No sensitive data** in published files

---

## 📦 What Gets Published

Files specified in `package.json` "files" field:
```json
"files": [
  "zima.js",                 // Main executable
  "postinstall.js",          // Setup script
  "ZIMA_GUIDE.md",           // User guide
  "TOOLS_REFERENCE.md",      // Tools documentation
  "README.md",               // npm page
  "README-INSTALL.md",       // Installation guide
  "INSTALL-QUICK.md"         // Quick reference
]
```

**NOT published** (via `.npmignore`):
- Test files
- Development scripts
- Example projects
- Git history
- node_modules

---

## 🌐 After Publishing

### Users can install with:

```bash
# Global installation (recommended)
npm install -g @zima-ai/zima

# Local installation
npm install @zima-ai/zima
```

### Then use:
```bash
zima
```

---

## 🔐 Package Scope

**Scoped package** (`@zima-ai/zima`) benefits:
- Namespace protection (prevents name conflicts)
- Organization branding
- Can have private packages (paid npm feature)

**First publish** of scoped package requires:
```bash
npm publish --access public
```

**Subsequent publishes**:
```bash
npm publish  # (remembers public access)
```

---

## 📊 npm Package Stats

After publishing, track:
- **Downloads**: https://npm-stat.com/charts.html?package=@zima-ai/zima
- **npm page**: https://www.npmjs.com/package/@zima-ai/zima
- **Unpkg CDN**: https://unpkg.com/@zima-ai/zima/

---

## 🚨 Common Issues

### "Package name taken"
- Change name in package.json
- Use scoped package: `@your-username/zima`

### "You must verify your email"
- Check npm account email
- Verify before publishing

### "npm publish failed: 402"
- Scoped packages need `--access public` on first publish

### "File not found after install"
- Check "files" field in package.json
- Check .npmignore isn't excluding needed files

---

## 🎯 Alternative: GitHub Packages

Publish to GitHub Packages instead:

1. **Create `.npmrc`**:
   ```
   @zima-ai:registry=https://npm.pkg.github.com
   ```

2. **Update package.json**:
   ```json
   "name": "@YOUR-GITHUB-USERNAME/zima",
   "repository": "git://github.com/YOUR-GITHUB-USERNAME/zima.git"
   ```

3. **Authenticate**:
   ```bash
   npm login --registry=https://npm.pkg.github.com
   ```

4. **Publish**:
   ```bash
   npm publish
   ```

---

## ✅ Quick Publish Commands

```bash
# First time publish
cd /Volumes/DATA/QWEN
npm login
npm publish --access public --dry-run  # Test first
npm publish --access public             # Real publish

# Update version and publish
npm version patch                       # or minor, or major
npm publish

# Unpublish (within 72 hours only)
npm unpublish @zima-ai/zima@3.0.0
```

---

## 📚 Resources

- **npm Docs**: https://docs.npmjs.com/
- **Semantic Versioning**: https://semver.org/
- **npm Package Best Practices**: https://docs.npmjs.com/packages-and-modules/contributing-packages-to-the-registry

---

**Ready to publish ZIMA to the world!** 🚀

Users will install with:
```bash
npm install -g @zima-ai/zima
```
