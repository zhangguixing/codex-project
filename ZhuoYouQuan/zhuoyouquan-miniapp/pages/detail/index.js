const api = require("../../utils/api");
Page({
  data: { item: null, loading: true, joining: false, isOwner: false, messageVisible: false, messageContent: "", sendingMessage: false, chats: [], allChats: [], chatTotal: 0, chatMessageTotal: 0, hiddenChatCount: 0, chatExpanded: false, chatToggleText: "", chatContent: "", chatSending: false, replyTarget: null, safetyVisible: false, safetyReason: "", reviewVisible: false, reviewContent: "", reviewScore: 5, broadcastVisible: false, broadcastContent: "", broadcasting: false, shareVisible: false, shareImagePath: "", posterGenerating: false },
  onLoad(query) { this.id = query.id; this.load(); },
  onShareAppMessage() { const item = this.data.item || {}; return { title: `来参加：${item.title || "桌游活动"}`, path: `/pages/detail/index?id=${this.id}`, imageUrl: item.coverUrl || undefined }; },
  onShareTimeline() { const item = this.data.item || {}; return { title: `${item.title || "桌游活动"}｜桌游圈`, query: `id=${this.id}`, imageUrl: item.coverUrl || undefined }; },
  back() {
    if (getCurrentPages().length > 1) wx.navigateBack();
    else wx.switchTab({ url: "/pages/home/index" });
  },
  load() { this.setData({ loading: true }); return Promise.all([api.activity(this.id), api.profile().catch(() => null)]).then(([item, user]) => { const isOwner = Boolean(user && item.organizer && user.id === item.organizer.id); const checkedIds = item.checkedInParticipantIds || []; item.participants = (item.participants || []).map((participant) => ({ ...participant, checkedIn: checkedIds.includes(participant.id) })); wx.setNavigationBarTitle({ title: item.title || "活动详情" }); this.setData({ item, isOwner }); return (isOwner || item.joined) ? api.activityChat(this.id).catch(() => []) : []; }).then((chats) => this.setChats(chats || [])).catch((e) => wx.showToast({ title: e.message, icon: "none" })).finally(() => this.setData({ loading: false })); },
  buildChatFloors(chats) { const floors = []; const byId = {}; chats.forEach((chat) => { if (!chat.parentId) { const floor = { ...chat, replies: [], repliesExpanded: false }; floors.push(floor); byId[chat.id] = floor; } }); floors.forEach((floor, index) => { floor.floor = index + 1; }); chats.forEach((chat) => { if (chat.parentId && byId[chat.parentId]) byId[chat.parentId].replies.push(chat); }); floors.forEach((floor) => { floor.replyCount = floor.replies.length; }); return floors; },
  chatViewData(allChats, chatExpanded) { const visibleLimit = 6; const hiddenChatCount = chatExpanded ? 0 : Math.max(allChats.length - visibleLimit, 0); return { chats: chatExpanded ? allChats : allChats.slice(0, visibleLimit), chatTotal: allChats.length, hiddenChatCount, chatExpanded, chatToggleText: chatExpanded ? "收起讨论" : `展开其余 ${hiddenChatCount} 层讨论` }; },
  setChats(chats) { const allChats = this.buildChatFloors(chats); this.setData({ allChats, chatMessageTotal: chats.length, ...this.chatViewData(allChats, false) }); },
  toggleChatCollapse() { const chatExpanded = !this.data.chatExpanded; this.setData(this.chatViewData(this.data.allChats || [], chatExpanded)); },
  toggleReplies(e) { const id = e.currentTarget.dataset.id; const allChats = (this.data.allChats || []).map((floor) => floor.id === id ? { ...floor, repliesExpanded: !floor.repliesExpanded } : floor); this.setData({ allChats, ...this.chatViewData(allChats, this.data.chatExpanded) }); },
  changeChat(e) { this.setData({ chatContent: e.detail.value }); },
  replyChat(e) { const chat = e.currentTarget.dataset.chat; if (!chat) return; this.setData({ replyTarget: chat, chatContent: "" }); },
  cancelReply() { this.setData({ replyTarget: null }); },
  sendChat() { const content = this.data.chatContent.trim(); if (!content || this.data.chatSending) return; const target = this.data.replyTarget; this.setData({ chatSending: true }); api.sendActivityChat(this.id, content, target && target.id).then(() => { this.setData({ chatContent: "", replyTarget: null }); return api.activityChat(this.id); }).then((chats) => this.setChats(chats || [])).catch((e) => wx.showToast({ title: e.message, icon: "none" })).finally(() => this.setData({ chatSending: false })); },
  checkIn(e) { const participant = e.currentTarget.dataset.participant; if (!participant || participant.checkedIn) return; api.checkInParticipant(this.id, participant.id).then(() => { wx.showToast({ title: "已确认到场", icon: "success" }); this.load(); }).catch((error) => wx.showToast({ title: error.message, icon: "none" })); },
  join() { if (!this.data.item || this.data.joining) return; this.setData({ joining: true }); api.joinActivity(this.data.item.id).then(() => { wx.showToast({ title: "报名成功", icon: "success" }); this.load(); }).catch((e) => wx.showToast({ title: e.message, icon: "none" })).finally(() => this.setData({ joining: false })); },
  cancelJoin() { if (!this.data.item || this.data.joining) return; wx.showModal({ title: "取消报名", content: this.data.item.waitlisted ? "确认退出候补吗？" : "确认取消报名吗？名额将优先递补给候补玩家。", confirmText: "确认取消", confirmColor: "#c8472d", success: (result) => { if (!result.confirm) return; this.setData({ joining: true }); api.cancelJoinActivity(this.data.item.id).then(() => { wx.showToast({ title: "已取消", icon: "success" }); this.load(); }).catch((e) => wx.showToast({ title: e.message, icon: "none" })).finally(() => this.setData({ joining: false })); }}); },
  navigateToLocation() {
    const item = this.data.item;
    const latitude = Number(item && item.latitude);
    const longitude = Number(item && item.longitude);
    if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) { wx.showToast({ title: "该活动暂未标注地图位置", icon: "none" }); return; }
    wx.openLocation({ latitude, longitude, name: item.storeName || item.title || "活动地点", address: item.address || "", scale: 18 });
  },
  openMessage() { this.setData({ messageVisible: true, messageContent: "" }); },
  noop() {},
  closeMessage() { if (!this.data.sendingMessage) this.setData({ messageVisible: false }); },
  changeMessage(e) { this.setData({ messageContent: e.detail.value }); },
  sendMessage() {
    const content = this.data.messageContent.trim();
    if (!content) { wx.showToast({ title: "请输入留言内容", icon: "none" }); return; }
    if (this.data.sendingMessage) return;
    this.setData({ sendingMessage: true });
    getApp().ensureSession()
      .then(() => api.leaveActivityMessage(this.data.item.id, content))
      .then(() => { this.setData({ messageVisible: false, messageContent: "" }); wx.showToast({ title: "留言已发送", icon: "success" }); })
      .catch((e) => wx.showToast({ title: e.message, icon: "none" }))
      .finally(() => this.setData({ sendingMessage: false }));
  },
  openSafety() { this.setData({ safetyVisible: true, safetyReason: "" }); },
  closeSafety() { this.setData({ safetyVisible: false }); },
  changeSafety(e) { this.setData({ safetyReason: e.detail.value }); },
  reportActivity() { const reason = this.data.safetyReason.trim(); if (!reason) { wx.showToast({ title: "请说明举报原因", icon: "none" }); return; } api.report({ targetType: "ACTIVITY", targetId: this.id, reason }).then(() => { this.closeSafety(); wx.showToast({ title: "举报已提交", icon: "success" }); }).catch((error) => wx.showToast({ title: error.message, icon: "none" })); },
  blockOrganizer() { const organizer = this.data.item && this.data.item.organizer; if (!organizer) return; wx.showModal({ title: "拉黑发起人", content: "拉黑后将不再收到对方相关消息。", confirmText: "确认拉黑", confirmColor: "#c8472d", success: (result) => { if (!result.confirm) return; api.blockUser(organizer.id).then(() => { this.closeSafety(); wx.showToast({ title: "已拉黑", icon: "success" }); }).catch((error) => wx.showToast({ title: error.message, icon: "none" })); } }); },
  openReview() { this.setData({ reviewVisible: true, reviewContent: "", reviewScore: 5 }); },
  closeReview() { this.setData({ reviewVisible: false }); },
  changeReview(e) { this.setData({ reviewContent: e.detail.value }); },
  changeReviewScore(e) { this.setData({ reviewScore: Number(e.detail.value) + 1 }); },
  submitReview() { const organizer = this.data.item && this.data.item.organizer; if (!organizer) return; const score = this.data.reviewScore; api.reviewActivity(this.id, { userId: organizer.id, punctualScore: score, friendlyScore: score, skillScore: score, communicationScore: score, content: this.data.reviewContent.trim() }).then(() => { this.closeReview(); wx.showToast({ title: "评价已提交", icon: "success" }); }).catch((error) => wx.showToast({ title: error.message, icon: "none" })); },
  openBroadcast() { this.setData({ broadcastVisible: true, broadcastContent: "" }); },
  closeBroadcast() { if (!this.data.broadcasting) this.setData({ broadcastVisible: false }); },
  changeBroadcast(e) { this.setData({ broadcastContent: e.detail.value }); },
  sendBroadcast() { const content = this.data.broadcastContent.trim(); if (!content || this.data.broadcasting) return; this.setData({ broadcasting: true }); api.broadcastActivity(this.id, content).then(() => { this.setData({ broadcastVisible: false }); wx.showToast({ title: "通知已发送", icon: "success" }); }).catch((error) => wx.showToast({ title: error.message, icon: "none" })).finally(() => this.setData({ broadcasting: false })); },
  openPoster() {
    if (!this.data.item || this.data.posterGenerating) return;
    this.setData({ posterGenerating: true });
    const item = this.data.item;
    api.activityShareCode(this.id)
      .then(({ imageBase64 }) => {
        const codePath = `${wx.env.USER_DATA_PATH}/activity-share-${this.id}.png`;
        wx.getFileSystemManager().writeFileSync(codePath, imageBase64, "base64");
        this.drawPoster(item, codePath);
      })
      .catch((error) => { this.setData({ posterGenerating: false }); wx.showToast({ title: error.message || "太阳码生成失败", icon: "none" }); });
  },
  drawPoster(item, codePath) {
    const context = wx.createCanvasContext("shareCanvas", this);
    context.setFillStyle("#f5f5f3"); context.fillRect(0, 0, 750, 1040); context.setFillStyle("#252629"); context.fillRect(0, 0, 750, 322);
    context.setFillStyle("#fff24b"); context.beginPath(); context.arc(638, 94, 126, 0, Math.PI * 2); context.fill(); context.setFillStyle("#252629"); context.setFontSize(70); context.fillText("局", 603, 120);
    context.setFillStyle("#fff35d"); context.setFontSize(25); context.fillText("TABLETOP INVITATION", 54, 68); context.setFillStyle("#fff"); context.setFontSize(52); context.fillText("今晚，一起开局", 52, 150); context.setFillStyle("#cfd1cc"); context.setFontSize(26); context.fillText("桌游圈邀请你加入这一桌", 54, 205);
    context.setFillStyle("#fff"); context.fillRect(42, 270, 666, 510); context.setFillStyle("#252629"); context.setFontSize(42); context.fillText(String(item.title || "桌游活动").slice(0, 18), 78, 360); context.setFillStyle("#d85143"); context.fillRect(78, 398, 104, 8);
    context.setFillStyle("#777a80"); context.setFontSize(25); context.fillText("活动时间", 78, 464); context.setFillStyle("#252629"); context.setFontSize(30); context.fillText(item.displayTime || "时间待定", 78, 510); context.setFillStyle("#777a80"); context.setFontSize(25); context.fillText("活动地点", 78, 580); context.setFillStyle("#252629"); context.setFontSize(29); context.fillText(String(item.storeName || item.address || "地点待定").slice(0, 24), 78, 626);
    context.setFillStyle("#fff24b"); context.fillRect(78, 674, 230, 64); context.setFillStyle("#252629"); context.setFontSize(27); context.fillText(`已报名 ${item.joinedPeople || 0} / ${item.maxPeople || "-"} 人`, 96, 716); context.setFillStyle("#252629"); context.setFontSize(25); context.fillText("打开小程序，查看详情并报名", 52, 890); context.setFillStyle("#9a9ca0"); context.setFontSize(22); context.fillText("桌游圈 · 找到同桌的人", 52, 936);
    context.drawImage(codePath, 520, 810, 150, 150);
    context.draw(false, () => wx.canvasToTempFilePath({ canvasId: "shareCanvas", width: 750, height: 1040, destWidth: 750, destHeight: 1040, success: (result) => this.setData({ shareImagePath: result.tempFilePath, shareVisible: true }), fail: () => wx.showToast({ title: "分享卡片生成失败", icon: "none" }), complete: () => this.setData({ posterGenerating: false }) }, this));
  },
  closePoster() { this.setData({ shareVisible: false }); },
  savePoster() { if (!this.data.shareImagePath) return; wx.saveImageToPhotosAlbum({ filePath: this.data.shareImagePath, success: () => wx.showToast({ title: "已保存到相册", icon: "success" }), fail: (error) => { if ((error.errMsg || "").includes("auth deny")) wx.showModal({ title: "需要相册权限", content: "开启权限后才能保存活动卡片", confirmText: "去设置", success: (result) => { if (result.confirm) wx.openSetting({}); } }); } }); },
  edit() { if (!this.data.item || this.data.item.status === "ENDED") { wx.showToast({ title: "已结束的活动不可编辑", icon: "none" }); return; } wx.setStorageSync("activityEditDraft", this.data.item); wx.switchTab({ url: "/pages/create/index" }); }
});
