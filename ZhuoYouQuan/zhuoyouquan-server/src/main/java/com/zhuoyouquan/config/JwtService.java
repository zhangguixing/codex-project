package com.zhuoyouquan.config;

import io.jsonwebtoken.Claims;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.security.Keys;
import org.springframework.stereotype.Service;

import javax.crypto.SecretKey;
import java.nio.charset.StandardCharsets;
import java.util.Date;

@Service
public class JwtService {
    private final JwtProperties properties;

    public JwtService(JwtProperties properties) {
        this.properties = properties;
    }

    private SecretKey key() {
        return Keys.hmacShaKeyFor(properties.getSecret().getBytes(StandardCharsets.UTF_8));
    }

    public String create(Long userId) {
        return Jwts.builder().subject(String.valueOf(userId)).issuedAt(new Date()).expiration(new Date(System.currentTimeMillis() + properties.getExpiresHours() * 3600_000)).signWith(key()).compact();
    }

    public Long parseUserId(String token) {
        Claims c = Jwts.parser().verifyWith(key()).build().parseSignedClaims(token).getPayload();
        return Long.valueOf(c.getSubject());
    }
}
