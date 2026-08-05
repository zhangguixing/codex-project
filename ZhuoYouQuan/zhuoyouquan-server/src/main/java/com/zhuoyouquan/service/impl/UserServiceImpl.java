package com.zhuoyouquan.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zhuoyouquan.common.BizException;
import com.zhuoyouquan.dto.UserProfileRequest;
import com.zhuoyouquan.entity.GameType;
import com.zhuoyouquan.entity.User;
import com.zhuoyouquan.entity.UserCredit;
import com.zhuoyouquan.entity.UserGame;
import com.zhuoyouquan.mapper.GameTypeMapper;
import com.zhuoyouquan.mapper.UserCreditMapper;
import com.zhuoyouquan.mapper.UserGameMapper;
import com.zhuoyouquan.mapper.UserMapper;
import com.zhuoyouquan.service.UserService;
import com.zhuoyouquan.vo.UserVO;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
public class UserServiceImpl implements UserService {
    private final UserMapper users;
    private final UserCreditMapper credits;
    private final UserGameMapper userGames;
    private final GameTypeMapper gameTypes;

    public UserServiceImpl(UserMapper users, UserCreditMapper credits, UserGameMapper userGames, GameTypeMapper gameTypes) {
        this.users = users;
        this.credits = credits;
        this.userGames = userGames;
        this.gameTypes = gameTypes;
    }

    @Override
    public UserVO getProfile(Long userId) {
        return toVO(requireUser(userId));
    }

    @Override
    @Transactional
    public UserVO updateProfile(Long userId, UserProfileRequest r) {
        User user = requireUser(userId);
        if (r.getNickname() != null) user.setNickname(r.getNickname());
        if (r.getAvatar() != null) user.setAvatar(r.getAvatar());
        if (r.getCity() != null) user.setCity(r.getCity());
        if (r.getDistrict() != null) user.setDistrict(r.getDistrict());
        if (r.getBio() != null) user.setBio(r.getBio());
        if (r.getGameLevel() != null) user.setGameLevel(r.getGameLevel());
        users.updateById(user);
        if (r.getGameTypeIds() != null) {
            userGames.delete(new LambdaQueryWrapper<UserGame>().eq(UserGame::getUserId, userId));
            for (Long gameTypeId : r.getGameTypeIds()) {
                if (gameTypes.selectById(gameTypeId) == null)
                    throw new BizException("桌游类型不存在"); UserGame x=new UserGame(); x.setUserId(userId); x.setGameTypeId(gameTypeId); userGames.insert(x); } }
                return toVO(user);
            }
            public User requireUser (Long id){
                User user = users.selectById(id);
                if (user == null) throw new BizException("用户不存在");
                return user;
            }
            public UserVO toVO (User u){
                UserVO v = new UserVO();
                v.setId(u.getId());
                v.setNickname(u.getNickname());
                v.setAvatar(u.getAvatar());
                v.setCity(u.getCity());
                v.setDistrict(u.getDistrict());
                v.setBio(u.getBio());
                v.setGameLevel(u.getGameLevel());
                UserCredit c = credits.selectOne(new LambdaQueryWrapper<UserCredit>().eq(UserCredit::getUserId, u.getId()));
                v.setCreditScore(c == null ? 100 : c.getScore());
                v.setCreditLevel(c == null ? "优秀玩家" : c.getLevel());
                List<Long> ids = userGames.selectList(new LambdaQueryWrapper<UserGame>().eq(UserGame::getUserId, u.getId())).stream().map(UserGame::getGameTypeId).toList();
                v.setFavoriteGames(ids.isEmpty() ? List.of() : gameTypes.selectBatchIds(ids).stream().map(GameType::getName).toList());
                return v;
            }
        }
