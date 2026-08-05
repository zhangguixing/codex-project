const api = require("../../utils/api");
Page({
  data: { item: null, loading: true, joining: false, isOwner: false },
  onLoad(query) { this.id = query.id; this.load(); },
  back() {
    if (getCurrentPages().length > 1) wx.navigateBack();
    else wx.switchTab({ url: "/pages/home/index" });
  },
  load() { this.setData({ loading: true }); return Promise.all([api.activity(this.id), api.profile().catch(() => null)]).then(([item, user]) => { wx.setNavigationBarTitle({ title: item.title || "活动详情" }); this.setData({ item, isOwner: Boolean(user && item.organizer && user.id === item.organizer.id) }); }).catch((e) => wx.showToast({ title: e.message, icon: "none" })).finally(() => this.setData({ loading: false })); },
  join() { if (!this.data.item || this.data.joining) return; this.setData({ joining: true }); api.joinActivity(this.data.item.id).then(() => { wx.showToast({ title: "报名成功", icon: "success" }); this.load(); }).catch((e) => wx.showToast({ title: e.message, icon: "none" })).finally(() => this.setData({ joining: false })); },
  edit() { wx.setStorageSync("activityEditDraft", this.data.item); wx.switchTab({ url: "/pages/create/index" }); }
});
