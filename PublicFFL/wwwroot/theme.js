window.theme = {
  set: (value) => {
    document.documentElement.setAttribute("data-theme", value);
    localStorage.setItem("theme", value);
  },
  get: () => localStorage.getItem("theme"),
  systemPrefersDark: () => window.matchMedia("(prefers-color-scheme: dark)").matches
};