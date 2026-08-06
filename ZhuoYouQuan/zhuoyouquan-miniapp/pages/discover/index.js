const api = require("../../utils/api");
const { isUpcomingActivity } = require("../../utils/activity");
Page({
  data: { activities: [], loading: true, filtersOpen: false, filters: { timeRange: "", difficulty: "", newbieFriendly: null, maxFee: "", minSlots: "" } },
  onShow() { const tabBar = this.getTabBar && this.getTabBar(); if (tabBar) tabBar.setSelected(1); this.load(); },
  toggleFilters() { this.setData({ filtersOpen: !this.data.filtersOpen }); },
  setFilter(e) { const { field, value } = e.currentTarget.dataset; const next = this.data.filters[field] === value ? (field === "newbieFriendly" ? null : "") : value; this.setData({ [`filters.${field}`]: next }); },
  changeFilter(e) { this.setData({ [`filters.${e.currentTarget.dataset.field}`]: e.detail.value }); },
  resetFilters() { this.setData({ filters: { timeRange: "", difficulty: "", newbieFriendly: null, maxFee: "", minSlots: "" } }, () => this.load()); },
  openStores() { wx.navigateTo({ url: "/pages/stores/index" }); },
  load() { const f = this.data.filters; const query = { sort: "latest" }; ["timeRange", "difficulty"].forEach((key) => { if (f[key]) query[key] = f[key]; }); if (f.newbieFriendly !== null) query.newbieFriendly = f.newbieFriendly; if (f.maxFee) query.maxFee = Number(f.maxFee); if (f.minSlots) query.minSlots = Number(f.minSlots); this.setData({ loading: true }); return api.activities(query).then((page) => this.setData({ activities: (page.records || []).filter(isUpcomingActivity) })).catch((e) => wx.showToast({ title: e.message, icon: "none" })).finally(() => this.setData({ loading: false })); }
});
