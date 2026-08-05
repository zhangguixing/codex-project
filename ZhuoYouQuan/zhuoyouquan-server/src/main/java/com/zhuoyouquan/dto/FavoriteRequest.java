package com.zhuoyouquan.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.Data;

@Data
public class FavoriteRequest {
    @NotBlank
    private String targetType;
    @NotNull
    private Long targetId;
}
