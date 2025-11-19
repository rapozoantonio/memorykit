# Security Policy

## Supported Versions

We actively support the following versions with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 0.1.x   | :white_check_mark: |
| < 0.1   | :x:                |

## Reporting a Vulnerability

We take the security of MemoryKit seriously. If you believe you have found a security vulnerability, please report it to us responsibly.

### How to Report

**DO NOT** open a public GitHub issue for security vulnerabilities.

Instead, please report security vulnerabilities through one of the following methods:

1. **GitHub Security Advisories** (Preferred)
   - Navigate to https://github.com/rapozoantonio/memorykit/security/advisories/new
   - Click "Report a vulnerability"
   - Fill in the details

2. **Email**
   - Send details to: security@memorykit.dev
   - Use PGP if possible (key available on request)

### What to Include

Please include the following information in your report:

- Type of vulnerability
- Full paths of source file(s) related to the vulnerability
- Location of the affected source code (tag/branch/commit/direct URL)
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the vulnerability
- Possible fixes (if you have any suggestions)

### Response Timeline

- **Initial Response**: Within 48 hours
- **Status Update**: Within 7 days
- **Fix Timeline**: Depends on severity
  - Critical: Within 7 days
  - High: Within 30 days
  - Medium: Within 90 days
  - Low: Next regular release

### What to Expect

1. **Acknowledgment**: We'll acknowledge receipt of your vulnerability report
2. **Validation**: We'll validate and reproduce the vulnerability
3. **Fix Development**: We'll develop and test a fix
4. **Disclosure**: We'll work with you on responsible disclosure
5. **Credit**: We'll credit you in the security advisory (if desired)

## Security Best Practices

When using MemoryKit, please follow these security best practices:

### API Keys and Secrets

- Never commit API keys, passwords, or secrets to version control
- Use environment variables or secure vaults (Azure Key Vault)
- Rotate API keys regularly
- Use separate keys for development and production

### Azure Resources

- Enable Azure AD authentication where possible
- Use Managed Identities for Azure resource access
- Enable Azure Security Center recommendations
- Configure network security groups appropriately
- Enable diagnostic logging

### Data Protection

- Always use TLS 1.3 for data in transit
- Enable encryption at rest for all Azure storage
- Implement proper data retention policies
- Follow GDPR and data protection regulations
- Use multi-tenancy isolation properly

### Application Security

- Keep dependencies up to date
- Run security scans regularly
- Implement rate limiting on API endpoints
- Validate and sanitize all inputs
- Use parameterized queries to prevent injection attacks
- Implement proper authentication and authorization
- Enable Application Insights for security monitoring

### Container Security

- Use official base images
- Run containers as non-root users
- Scan container images for vulnerabilities
- Keep container runtime and orchestration updated
- Implement resource limits
- Use read-only file systems where possible

## Security Features

MemoryKit implements the following security features:

### Authentication & Authorization

- API Key authentication
- Azure AD integration support
- Role-based access control (RBAC)
- Per-user data isolation

### Data Protection

- AES-256 encryption at rest
- TLS 1.3 for data in transit
- Secure key management with Azure Key Vault
- Automatic data deletion (GDPR compliance)

### Security Scanning

- Automated dependency vulnerability scanning
- Container image scanning with Trivy
- CodeQL static analysis
- OWASP dependency check

### Monitoring & Logging

- Application Insights integration
- Security event logging
- Anomaly detection
- Audit trails

## Vulnerability Disclosure Policy

We follow responsible disclosure principles:

1. **Private Disclosure**: Report to us first, not publicly
2. **Investigation**: Give us reasonable time to investigate and fix
3. **Coordinated Disclosure**: Work with us on timing public disclosure
4. **Credit**: We'll credit researchers in security advisories
5. **No Legal Action**: We won't pursue legal action for good-faith security research

## Scope

### In Scope

- MemoryKit API
- MemoryKit infrastructure components
- Azure deployment configurations
- Sample applications
- CI/CD pipelines

### Out of Scope

- Third-party dependencies (report to their maintainers)
- Azure platform vulnerabilities (report to Microsoft)
- Social engineering attacks
- Physical attacks
- Denial of service attacks

## Security Updates

Security updates will be:

- Released as patch versions (e.g., 0.1.1)
- Documented in security advisories
- Announced through GitHub releases
- Included in the CHANGELOG.md

## Bug Bounty Program

We currently do not have a bug bounty program, but we greatly appreciate security research and will:

- Publicly acknowledge your contribution
- Credit you in security advisories
- Consider your findings for future bounty programs

## Questions?

If you have questions about this security policy, please contact:
- Email: security@memorykit.dev
- GitHub: Open a discussion at https://github.com/rapozoantonio/memorykit/discussions

## Hall of Fame

We thank the following security researchers for their responsible disclosure:

<!-- Names will be added here as vulnerabilities are reported and fixed -->

---

**Thank you for helping keep MemoryKit and our users safe!**
