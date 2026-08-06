const api = require("../../utils/api");
const { isUpcomingActivity } = require("../../utils/activity");

function cityFromAddress(address) {
  const match = (address || "").match(/(?:[^省]+省)?([^市]+市)/);
  return match ? match[1] : "";
}

Page({
  data: { activities: [], types: [], selectedType: "", timeRange: "", city: "", keyword: "", locationLabel: "定位中", loading: true, locating: false },
  onLoad() { this.requestLocationPermission(); api.gameTypes().then((types) => this.setData({ types })).catch(this.showError); },
  onShow() { const tabBar = this.getTabBar && this.getTabBar(); if (tabBar) tabBar.setSelected(0); },
  onPullDownRefresh() { this.load().finally(() => wx.stopPullDownRefresh()); },
  load() {
    const requestId = (this.requestId || 0) + 1;
    this.requestId = requestId;
    const location = wx.getStorageSync("homeLocation") || {};
    this.setData({ loading: true });
    return api.activities({ city: this.data.city, keyword: this.data.keyword || undefined, gameTypeId: this.data.selectedType || undefined, timeRange: this.data.timeRange || undefined, latitude: location.latitude, longitude: location.longitude, sort: "latest" })
      .then((page) => { if (requestId === this.requestId) this.setData({ activities: (page.records || []).filter(isUpcomingActivity) }); })
      .catch((error) => { if (requestId === this.requestId) this.showError(error); })
      .finally(() => { if (requestId === this.requestId) this.setData({ loading: false }); });
  },
  chooseType(e) { this.setData({ selectedType: e.currentTarget.dataset.id }, () => this.load()); },
  chooseTime(e) { this.setData({ timeRange: e.currentTarget.dataset.range }, () => this.load()); },
  createActivity() { wx.switchTab({ url: "/pages/create/index" }); },
  changeKeyword(e) {
    const keyword = e.detail.value;
    this.keyword = keyword;
    this.setData({ keyword });
    if (!keyword) this.load();
  },
  search() {
    if (this.keyword !== undefined && this.keyword !== this.data.keyword) this.setData({ keyword: this.keyword }, () => this.load());
    else this.load();
  },
  requestLocationPermission() {
    wx.getSetting({
      success: ({ authSetting }) => {
        const locationAuthorized = authSetting["scope.userLocation"];
        if (locationAuthorized) { this.locate(); return; }
        if (locationAuthorized === false) { this.locationUnavailable(); return; }
        wx.authorize({
          scope: "scope.userLocation",
          success: () => this.locate(),
          fail: () => this.locationUnavailable()
        });
      },
      fail: () => this.locate()
    });
  },
  locationUnavailable() {
    this.setData({ locationLabel: "\u8bf7\u5730\u56fe\u9009\u62e9", locating: false }, () => this.load());
  },
  locate() {
    this.setData({ locating: true, locationLabel: "定位中" });
    wx.getLocation({
      type: "gcj02",
      success: (location) => {
        wx.setStorageSync("homeLocation", { latitude: location.latitude, longitude: location.longitude });
        this.setData({ locationLabel: "已获取", locating: false }, () => this.load());
      },
      fail: () => {
        this.setData({ locationLabel: "请地图选择", locating: false }, () => this.load());
      }
    });
  },
  chooseLocation() {
    wx.chooseLocation({
      success: (location) => {
        const city = cityFromAddress(location.address) || this.data.city;
        wx.setStorageSync("homeLocation", { latitude: location.latitude, longitude: location.longitude });
        this.setData({ city, locationLabel: city || location.name || "已选择地图位置" }, () => this.load());
      },
      fail: (error) => {
        if (!/cancel/.test((error && error.errMsg) || "")) wx.showToast({ title: "地图位置选择失败", icon: "none" });
      }
    });
  },
  showError(error) { wx.showToast({ title: error.message || "加载失败", icon: "none" }); }
});
