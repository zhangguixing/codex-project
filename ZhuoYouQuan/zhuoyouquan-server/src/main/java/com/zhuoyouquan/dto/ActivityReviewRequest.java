package com.zhuoyouquan.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import lombok.Data;

@Data
public class ActivityReviewRequest {
    @NotNull
    private Long userId;
    @NotNull
    @Min(1)
    @Max(5)
    private Integer punctualScore;
    @NotNull
    @Min(1)
    @Max(5)
    private Integer friendlyScore;
    @NotNull
    @Min(1)
    @Max(5)
    private Integer skillScore;
    @NotNull
    @Min(1)
    @Max(5)
    private Integer communicationScore;
    @Size(max = 500)
    private String content;
}
