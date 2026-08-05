Component({
  properties: { item: { type: Object, value: {} } },
  methods: {
    open() { wx.navigateTo({ url: `/pages/detail/index?id=${this.properties.item.id}` }); }
  }
});
