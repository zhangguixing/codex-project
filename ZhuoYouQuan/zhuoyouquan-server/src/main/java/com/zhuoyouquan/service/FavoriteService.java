package com.zhuoyouquan.service;

import com.zhuoyouquan.dto.FavoriteRequest;

public interface FavoriteService {
    void add(Long userId, FavoriteRequest request);

    void remove(Long userId, String targetType, Long targetId);
}
