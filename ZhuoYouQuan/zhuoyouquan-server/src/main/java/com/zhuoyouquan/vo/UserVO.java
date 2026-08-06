package com.zhuoyouquan.vo;

import lombok.Data;

import java.util.List;

@Data
public class UserVO {
    private Long id;
    private String nickname;
    private String avatar;
    private String city;
    private String district;
    private String bio;
    private String gameLevel;
    private Integer creditScore;
    private String creditLevel;
    private List<String> favoriteGames;
}
