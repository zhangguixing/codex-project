const items = [
  { text: "附近", path: "/pages/home/index", icon: "/assets/nav/map-pin.svg" },
  { text: "发现", path: "/pages/discover/index", icon: "/assets/nav/compass.svg" },
  { text: "开局", path: "/pages/create/index", icon: "/assets/nav/circle-plus.svg", primary: true },
  { text: "消息", path: "/pages/messages/index", icon: "/assets/nav/bell.svg" },
  { text: "我的", path: "/pages/profile/index", icon: "/assets/nav/user-round.svg" }
];

Component({
  data: { items: items.map((item, index) => ({ ...item, active: index === 0 })), selected: 0, hidden: false },
  lifetimes: {
    attached() { this.syncSelected(); }
  },
  methods: {
    syncSelected() {
      const pages = getCurrentPages();
      const current = pages[pages.length - 1];
      const index = items.findIndex((item) => item.path.slice(1) === (current && current.route));
      this.setSelected(index >= 0 ? index : 0);
    },
    setSelected(selected, callback) {
      this.setData({ selected, items: items.map((item, index) => ({ ...item, active: index === selected })) }, callback);
    },
    switchTab(event) {
      const { path, index } = event.currentTarget.dataset;
      const selected = Number(index);
      if (selected === this.data.selected) return;
      this.setSelected(selected, () => {
        wx.switchTab({ url: path, fail: () => this.syncSelected() });
      });
    }
  }
});
