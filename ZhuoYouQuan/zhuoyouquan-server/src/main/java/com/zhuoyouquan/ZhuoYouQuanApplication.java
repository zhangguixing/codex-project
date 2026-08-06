package com.zhuoyouquan;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableScheduling;

@SpringBootApplication
@EnableScheduling
public class ZhuoYouQuanApplication {
    public static void main(String[] args) {
        SpringApplication.run(ZhuoYouQuanApplication.class, args);
    }
}
