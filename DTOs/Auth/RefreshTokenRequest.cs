namespace glint_backend.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        /// <summary>
        /// Optional when the refresh token is sent as an HttpOnly cookie instead.
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// When true and RefreshToken is supplied in the body, response will include new HttpOnly session cookies.
        /// </summary>
        public bool PromoteToCookieSession { get; set; }
    }
}
