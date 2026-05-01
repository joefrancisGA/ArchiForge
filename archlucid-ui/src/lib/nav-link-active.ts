/**
 * Whether a sidebar / drawer link should show the active style for the current pathname.
 * Query strings on `href` are ignored; pathname never includes query in Next.js App Router.
 */
export function isNavLinkActive(pathname: string, href: string): boolean {
  const pathPart = href.split("?")[0] ?? "/";

  if (pathPart === "/") {
    return pathname === "/";
  }

  if (pathPart === "/reviews/new") {
    return pathname === "/reviews/new";
  }

  if (pathPart === "/reviews") {
    return pathname === "/reviews";
  }

  return pathname === pathPart || pathname.startsWith(`${pathPart}/`);
}
