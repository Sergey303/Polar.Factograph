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

function activeOpenMenu(menus: HTMLDetailsElement[]): HTMLDetailsElement | undefined {
  for (let index = menus.length - 1; index >= 0; index -= 1) {
    const menu = menus[index];
    if (menu?.contains(document.activeElement)) return menu;
  }
  return menus[menus.length - 1];
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

  const activeMenu = activeOpenMenu(openMenus());
  if (activeMenu === undefined) return;

  event.preventDefault();
  closeMenu(activeMenu, true);
});
