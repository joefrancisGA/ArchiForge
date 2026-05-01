/**
 * Routes backed by **`surface: "platform-admin"`** nav groups — expand the Administration
 * sidebar section when the user deep-links (so diagnostics are not hidden behind a blind toggle).
 */
export function pathnameTouchesPlatformAdminSurface(pathname: string): boolean {
  if (pathname.startsWith("/admin")) {
    return true;
  }

  if (pathname.startsWith("/settings/tenant-cost")) {
    return true;
  }

  if (pathname.startsWith("/settings/baseline")) {
    return true;
  }

  if (pathname === "/settings/tenant" || pathname.startsWith("/settings/tenant/")) {
    return true;
  }

  if (pathname.startsWith("/settings/exec-digest")) {
    return true;
  }

  return false;
}
