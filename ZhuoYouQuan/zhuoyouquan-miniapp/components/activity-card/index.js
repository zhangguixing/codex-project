Component({
  properties: { item: { type: Object, value: {} }, showEndAction: { type: Boolean, value: false }, ending: { type: Boolean, value: false } },
  methods: {
    open() { wx.navigateTo({ url: `/pages/detail/index?id=${this.properties.item.id}` }); },
    end(event) { this.triggerEvent("end", { id: event.currentTarget.dataset.id }); }
  }
});
