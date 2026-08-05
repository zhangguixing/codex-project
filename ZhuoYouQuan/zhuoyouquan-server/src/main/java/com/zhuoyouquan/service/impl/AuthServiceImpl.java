package com.zhuoyouquan.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zhuoyouquan.common.BizException;
import com.zhuoyouquan.config.JwtService;
import com.zhuoyouquan.config.WechatProperties;
import com.zhuoyouquan.entity.User;
import com.zhuoyouquan.entity.UserCredit;
import com.zhuoyouquan.mapper.UserCreditMapper;
import com.zhuoyouquan.mapper.UserMapper;
import com.zhuoyouquan.service.AuthService;
import com.zhuoyouquan.vo.AuthVO;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClient;

import java.time.LocalDateTime;

@Service
public class AuthServiceImpl implements AuthService {
    private final UserMapper users;
    private final UserCreditMapper credits;
    private final UserServiceImpl userService;
    private final JwtService jwt;
    private final WechatProperties wechat;
    private final ObjectMapper mapper;

    public AuthServiceImpl(UserMapper users, UserCreditMapper credits, UserServiceImpl userService, JwtService jwt, WechatProperties wechat, ObjectMapper mapper) {
        this.users = users;
        this.credits = credits;
        this.userService = userService;
        this.jwt = jwt;
        this.wechat = wechat;
        this.mapper = mapper;
    }

    @Override
    public AuthVO login(String code) {
        String openid = resolveOpenid(code);
        User user = users.selectOne(new LambdaQueryWrapper<User>().eq(User::getOpenid, openid));
        if (user == null) {
            user = new User();
            user.setOpenid(openid);
            user.setNickname("桌游玩家");
            user.setGameLevel("新手玩家");
            user.setCreatedAt(LocalDateTime.now());
            users.insert(user);
            UserCredit credit = new UserCredit();
            credit.setUserId(user.getId());
            credit.setScore(100);
            credit.setLevel("优秀玩家");
            credit.setUpdatedAt(LocalDateTime.now());
            credits.insert(credit);
        }
        user.setLastLoginAt(LocalDateTime.now());
        users.updateById(user);
        return new AuthVO(jwt.create(user.getId()), userService.toVO(user));
    }

    private String resolveOpenid(String code) {
        if (wechat.isMockLogin()) return "dev_miniapp_user";
        if (wechat.getAppId() == null || wechat.getAppId().isBlank() || wechat.getAppSecret() == null || wechat.getAppSecret().isBlank())
            throw new BizException("微信小程序凭证未配置");
        try {
            String body = RestClient.create().get().uri("https://api.weixin.qq.com/sns/jscode2session?appid={id}&secret={secret}&js_code={code}&grant_type=authorization_code", wechat.getAppId(), wechat.getAppSecret(), code).retrieve().body(String.class);
            JsonNode node = mapper.readTree(body);
            if (node.has("errcode"))
                throw new BizException("微信登录失败：" + node.path("errmsg").asText());return node.path("openid").asText();} catch(BizException e){throw e;}catch(Exception e){throw new BizException("微信服务不可用");
        }
    }
}
