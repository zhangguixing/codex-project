package com.zhuoyouquan.dto;

import jakarta.validation.constraints.Size;
import lombok.Data;

import java.util.List;

@Data
public class UserProfileRequest {
    @Size(max = 32)
    private String nickname;
    private String avatar;
    @Size(max = 32)
    private String city;
    @Size(max = 32)
    private String district;
    @Size(max = 200)
    private String bio;
    private String gameLevel;
    private List<Long> gameTypeIds;
}
