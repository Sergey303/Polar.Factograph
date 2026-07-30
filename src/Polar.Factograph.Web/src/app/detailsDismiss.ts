const dismissibleSelector = [
  ".semantic-sections-menu[open]",
  ".block-layout-menu[open]"
].join(", ");

function openMenus(): HTMLDetailsElement[] {
  return [...document.querySelectorAll<HTMLDetailsElement>(dismissibleSelector)];
}

function closeMenu(menu: HTMLDetailsElement, restoreFocus: boolean): void {
  menu.open = false;
  if (restoreFocus) {
    menu.querySelector<HTMLElement>(":scope > summary")?.focus();
  }
}

document.addEventListener("pointerdown", event => {
  const target = event.target;
  if (!(target instanceof Node)) return;

  for (const menu of openMenus()) {
    if (!menu.contains(target)) closeMenu(menu, false);
  }
});

document.addEventListener("keydown", event => {
  if (event.key !== "Escape") return;

  const menus = openMenus();
  const activeMenu = menus.findLast(menu => menu.contains(document.activeElement)) ??
    menus.at(-1);
  if (activeMenu === undefined) return;

  event.preventDefault();
  closeMenu(activeMenu, true);
});
