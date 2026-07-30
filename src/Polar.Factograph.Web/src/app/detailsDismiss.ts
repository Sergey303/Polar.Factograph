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

function scrollToUpdatedBlock(button: HTMLButtonElement): void {
  const navigation = button.closest<HTMLElement>(".semantic-block-pagination");
  const section = button.closest<HTMLElement>(".semantic-content-block");
  if (navigation === null || section === null) return;

  const status = navigation.querySelector<HTMLElement>("span");
  status?.setAttribute("aria-live", "polite");
  status?.setAttribute("aria-atomic", "true");

  window.requestAnimationFrame(() => {
    section.scrollIntoView({
      block: "start",
      behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches
        ? "auto"
        : "smooth"
    });
  });
}

document.addEventListener("pointerdown", event => {
  const target = event.target;
  if (!(target instanceof Node)) return;

  for (const menu of openMenus()) {
    if (!menu.contains(target)) closeMenu(menu, false);
  }
});

document.addEventListener("click", event => {
  const target = event.target;
  if (!(target instanceof Element)) return;

  const button = target.closest<HTMLButtonElement>(".semantic-block-pagination button");
  if (button === null || button.disabled) return;
  scrollToUpdatedBlock(button);
});

document.addEventListener("keydown", event => {
  if (event.key !== "Escape") return;

  const activeMenu = activeOpenMenu(openMenus());
  if (activeMenu === undefined) return;

  event.preventDefault();
  closeMenu(activeMenu, true);
});
