> **Scope:** Security custodian (owner) generating, exporting, publishing, and rotating the OpenPGP key used for coordinated disclosure to **`security@archlucid.com`**. Not Stripe/Marketplace secrets, not CI automation of private keys.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# PGP key generation and publication (coordinated disclosure)

## Purpose

ArchLucid publishes a **public** OpenPGP key so vulnerability reporters can **encrypt** findings to **`security@archlucid.com`** before the public key exists at **`https://archlucid.com/.well-known/pgp-key.txt`**, coordinated disclosure uses **plain email** only (see [SECURITY.md](../../SECURITY.md)).

This document is an **executable recipe** for the **owner-self custodian** (decision **2026-04-22**, [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) items **10** / **21**). The **private** key never enters this repository, CI secrets, or Azure Key Vault (Key Vault is for application secrets such as Stripe/Marketplace—not PGP private material).

## Prerequisites

1. Install **GnuPG 2.4.x** (or newer **2.x**) on the machine where the private key will live.

   ```bash
   gpg --version
   ```

   If the major menu text below does not match your build, run `gpg --full-generate-key` once in a throwaway profile and map the same **semantic** choices (ECC sign+encrypt vs RSA 4096, Curve25519 vs RSA length, UID, expiration).

2. Prepare a **strong passphrase** in the owner password manager (**1Password** or equivalent). You will paste it only into the GnuPG prompt (or passphrase file for batch mode)—never into Slack, email, or the repo.

## Choose algorithm

| Choice | When to use |
| ------ | ------------- |
| **ECC (Curve25519)** — GnuPG menu “ECC (sign and encrypt)” | **Preferred:** smaller keys, modern defaults, fine for almost all reporters using current GnuPG or OpenPGP-capable clients. |
| **RSA 4096** | **Fallback** if a procurement or legal counterparty’s policy still mandates RSA (legacy compatibility). |

**Default for ArchLucid:** use **ECC / Curve25519** unless you have a written requirement to use RSA.

## Generate (ECC sign and encrypt, Curve25519) — interactive

Run:

```bash
gpg --full-generate-key
```

Use these answers (GnuPG **2.4.x** style menus; if numbering differs, pick the line that reads like **ECC (sign and encrypt)** and **Curve 25519**):

1. **What kind of key?** Choose **ECC (sign and encrypt)** (often option **9**). Do **not** choose “sign only” unless you know how to add a separate encryption subkey.

2. **Which elliptic curve?** Choose **Curve 25519** (often option **1**, marked default).

3. **Expiration:** Enter **`0`** for **no expiration** (simplest for a long-lived `security@` key). **Owner may override:** use **`5y`** for a five-year key; set a calendar reminder at **4.5 years** to generate a successor and publish a new `pgp-key.txt` (see [Rotation](#rotation)).

4. **Real name:** `ArchLucid Security`

5. **Email address:** `security@archlucid.com`

6. **Comment:** leave **empty** (press Enter).

7. **Passphrase:** paste the value from your password manager (minimum **128 bits** of entropy in practice—long random sentence or generator output).

8. Confirm with **`y`**.

Confirm the key exists:

```bash
gpg -K security@archlucid.com
```

Record the **full fingerprint** (40 hex chars, spaces in groups of 4) in the table at the end of this file.

## Generate (RSA 4096) — interactive

Run:

```bash
gpg --full-generate-key
```

1. **What kind of key?** Choose **RSA and RSA** (often option **1**).

2. **RSA key size:** enter **`4096`**.

3. **Expiration:** **`0`** (no expiry) or **`5y`** (same override note as above).

4. **Real name / Email / Comment:** same as ECC: **`ArchLucid Security`**, **`security@archlucid.com`**, empty comment.

5. **Passphrase:** same discipline as ECC.

6. **`gpg -K security@archlucid.com`** and record the fingerprint in the table below.

## Generate (non-interactive batch) — optional

If you already have a passphrase file on disk **only for the duration of this command** (e.g. `/tmp/pw` on an offline machine, deleted immediately after):

**ECC (sign+encrypt) default curve (Curve25519 family on modern GnuPG):**

```bash
gpg --batch --pinentry-mode loopback --passphrase-file /path/to/passphrase.txt \
  --quick-gen-key "ArchLucid Security <security@archlucid.com>" default default 0
```

The final **`0`** means **no expiration** (use **`5y`** for five years if you chose that policy).

**RSA 4096:**

```bash
gpg --batch --pinentry-mode loopback --passphrase-file /path/to/passphrase.txt \
  --quick-gen-key "ArchLucid Security <security@archlucid.com>" rsa4096 default 0
```

Then **`shred -u /path/to/passphrase.txt`** (Linux) or secure-delete on macOS/Windows.

If `--quick-gen-key` errors on your version, use the **interactive** section only—that is the supported path.

## Export public key (ASCII armor)

From the repo root (adjust path if your shell is elsewhere). Prefer export by **UID** once you know the key exists:

```bash
gpg --armor --export "security@archlucid.com" > archlucid-ui/public/.well-known/pgp-key.txt
```

If you have **multiple** keys for that UID, export by **fingerprint** (no spaces in the argument). Copy the **40-character** primary fingerprint from `gpg -K security@archlucid.com` (hex only, no spaces), then:

```bash
# Replace the placeholder with your real 40-hex fingerprint from gpg (do not commit secrets).
gpg --armor --export "PASTE_PRIMARY_FINGERPRINT_40_HEX_NO_SPACES" > archlucid-ui/public/.well-known/pgp-key.txt
```

Inspect the file:

```bash
head -n 5 archlucid-ui/public/.well-known/pgp-key.txt
tail -n 3 archlucid-ui/public/.well-known/pgp-key.txt
```

You must see **`-----BEGIN PGP PUBLIC KEY BLOCK-----`** at the top and **`-----END PGP PUBLIC KEY BLOCK-----`** at the bottom.

## Publish (repository + buyer-facing URL)

1. **Commit** `archlucid-ui/public/.well-known/pgp-key.txt` on the default branch.

2. **CI:** [`scripts/ci/assert_pgp_key_present.py`](../../scripts/ci/assert_pgp_key_present.py) treats a **missing** key as **pending** (warn-only). Once the file exists, it must be a **well-formed armored public key block**; a broken file fails the guard—fix or revert before merge.

3. **After merge**, confirm the marketing host serves **`/.well-known/pgp-key.txt`** over **HTTPS** (same path as source).

4. **Fingerprint (short form) for humans:** take the **last 16 hex digits** of the fingerprint (sometimes called **64-bit key id**). Append to [SECURITY.md](../../SECURITY.md) under the PGP section and to [TRUST_CENTER.md](../go-to-market/TRUST_CENTER.md) Contact bullets using a short line such as **Key ID** followed by those sixteen digits from `gpg --fingerprint` (do not paste sample key material into git; use only your real values after generation).

5. Update the **custodian record** table at the bottom of **this** file with the full fingerprint.

## Rotation

- **Cadence:** if you chose **no expiration**, still review annually whether the key should be rotated (staff turnover, device loss, or policy change). If you chose **`5y`**, start successor-key work **six months** before expiry.

- **Successor key:** generate a new keypair (same UID is acceptable; distinguish by fingerprint), export a **new** `pgp-key.txt` (or append multiple armored blocks—prefer **replace file** with the current canonical key unless you are intentionally publishing a transition bundle), commit, update fingerprints in `SECURITY.md` / `TRUST_CENTER.md`.

## Revocation

1. Generate a **revocation certificate** immediately after key creation (before you forget):

   ```bash
   gpg --output ~/archlucid-security-revoke-PRIMARY.asc --gen-revoke security@archlucid.com
   ```

   Choose a reason code GnuPG offers (usually **1 = key superseded** or **3 = key compromised** when the time comes). Store **`archlucid-security-revoke-PRIMARY.asc`** in the **same vault** as the passphrase (not in the repo).

2. **If the private key is compromised:** import the revocation certificate on a trusted machine with the **master** key present, or publish revocation to keyservers per your incident runbook, then publish an updated buyer notice in `SECURITY.md`.

## Where the private key lives

| Allowed | Not allowed |
| ------- | ----------- |
| Owner **hardware-backed** store (**YubiKey**, **Nitrokey**, TPM-backed GnuPG, or encrypted offline backup controlled by the custodian) | **Never** commit private keys, `.asc` secret key exports, or `private-keys-v1.d` blobs to git |
| Owner **laptop** keyring **only** if full-disk encryption and device policy match your threat model | **Never** store the private key material in **Azure Key Vault**, **GitHub Actions secrets**, or shared drives |
| **Paper** backup of revocation certificate + fingerprint in a physical safe (optional) | **Never** email the private key to yourself |

---

## Custodian record (owner-maintained)

| Field | Value |
| ----- | ----- |
| **UID** | `ArchLucid Security <security@archlucid.com>` |
| **Algorithm** | *(owner: ECC Curve25519 or RSA 4096)* |
| **Full fingerprint** | *(owner: paste 40 hex chars after generation)* |
| **Expiration choice** | *(owner: `none` or date from `gpg -K`)* |
| **Revocation cert location** | *(owner: vault path / reference only—no secret contents here)* |
| **Date first published to repo** | *(owner: ISO date when `pgp-key.txt` merged)* |
