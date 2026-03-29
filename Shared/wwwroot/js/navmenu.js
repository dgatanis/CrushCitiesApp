window.collapseNavbar = (selector) => {
  const el = document.querySelector(selector);
  if (!el) return;
  const bsCollapse = bootstrap.Collapse.getOrCreateInstance(el);
  bsCollapse.hide();
};

window.isNavbarTogglerVisible = () => {
  const toggler = document.querySelector(".navbar-toggler");
  if (!toggler) return false;
  return window.getComputedStyle(toggler).display !== "none";
};
