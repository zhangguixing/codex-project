package com.zhuoyouquan.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zhuoyouquan.common.BizException;
import com.zhuoyouquan.dto.FavoriteRequest;
import com.zhuoyouquan.entity.Favorite;
import com.zhuoyouquan.mapper.FavoriteMapper;
import com.zhuoyouquan.service.FavoriteService;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.Set;

@Service
public class FavoriteServiceImpl implements FavoriteService {
    private static final Set<String> TYPES = Set.of("ACTIVITY", "PLAYER", "STORE");
    private final FavoriteMapper favorites;

    public FavoriteServiceImpl(FavoriteMapper favorites) {
        this.favorites = favorites;
    }

    public void add(Long userId, FavoriteRequest r) {
        String type = r.getTargetType().toUpperCase();
        if (!TYPES.contains(type)) throw new BizException("不支持的收藏类型");
        if (favorites.selectCount(new LambdaQueryWrapper<Favorite>().eq(Favorite::getUserId, userId).eq(Favorite::getTargetType, type).eq(Favorite::getTargetId, r.getTargetId())) > 0)
            return;
        Favorite f = new Favorite();
        f.setUserId(userId);
        f.setTargetType(type);
        f.setTargetId(r.getTargetId());
        f.setCreatedAt(LocalDateTime.now());
        favorites.insert(f);
    }

    public void remove(Long userId, String type, Long targetId) {
        favorites.delete(new LambdaQueryWrapper<Favorite>().eq(Favorite::getUserId, userId).eq(Favorite::getTargetType, type.toUpperCase()).eq(Favorite::getTargetId, targetId));
    }
}
