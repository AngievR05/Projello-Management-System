using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Projello.Api.DTOs;
using OtpNet; // Uses the same TOTP engine your controller uses to create live matching test tokens
using Xunit;

namespace Projello.Api.Tests.Integration
{
    // WebApplicationFactory boots your REAL API in the background
    public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public AuthIntegrationTests(WebApplicationFactory<Program> factory)
        {
            // Creates a real HTTP client pointing at your background API
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RegisterCompany_WithValidData_CreatesCompanyAndUser()
        {
            // Arrange: Generate a unique email so the test passes every time you run it
            var uniqueEmail = $"owner_{Guid.NewGuid()}@test.com";
            
            var registerDto = new UserRegisterDto
            {
                CompanyName = "Real Integration Corp",
                FullName = "Integration Boss",
                Email = uniqueEmail,
                Password = "Password123!"
            };

            // Act: Fire a REAL HTTP POST to your register-company endpoint
            var response = await _client.PostAsJsonAsync("/api/auth/register-company", registerDto);

            // Assert: Check that we got a 200 OK back
            response.EnsureSuccessStatusCode(); 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Contains("Company registered successfully", responseData);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsRealJwtToken()
        {
            // Arrange: 1. Create the user first so we know they exist in the DB
            var uniqueEmail = $"login_{Guid.NewGuid()}@test.com";
            var password = "SuperSecretPassword123!";
            
            var registerDto = new UserRegisterDto
            {
                FullName = "Login Tester",
                Email = uniqueEmail,
                Password = password
            };
            
            // Note: Using standard register here, not company register
            await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // 2. Prepare the Login payload
            var loginDto = new UserLoginDto
            {
                Email = uniqueEmail,
                Password = password
            };

            // Act: Fire a REAL HTTP POST to your login endpoint
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            loginResponse.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            // Verify the JWT token is actually attached to the response
            var authData = await loginResponse.Content.ReadFromJsonAsync<LoginResponseData>();
            
            Assert.NotNull(authData);
            Assert.False(string.IsNullOrEmpty(authData.Token)); // Proves the JWT was generated!
            Assert.Equal("Login Tester", authData.User);        // Proves the user data came back!
            Assert.False(authData.Requires2FA);                 // Proves 2FA logic fired correctly
        }

        [Fact]
        public async Task Login_WithWrongPassword_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                Email = "thisuserdoesnotexist@test.com",
                Password = "WrongPassword!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Contains("Invalid credentials", responseData);
        }

        // ==================== NEW 2FA BRANCH COVERAGE TESTS ====================

        [Fact]
        public async Task Generate2FASecret_MismatchedEmail_ReturnsForbidden()
        {
            var uniqueEmail = $"2fa_mismatch_{Guid.NewGuid()}@test.com";
            var password = "Password123!";
            await _client.PostAsJsonAsync("/api/auth/register", new UserRegisterDto { FullName = "Mismatch", Email = uniqueEmail, Password = password });

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto { Email = uniqueEmail, Password = password });
            var authData = await loginResponse.Content.ReadFromJsonAsync<LoginResponseData>();

            // Authenticate the HTTP client with the bearer token
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authData!.Token);
            
            // Post an entirely different email address to trip the Forbid validation branch
            var response = await _client.PostAsJsonAsync("/api/auth/generate-2fa-secret", new { Email = "wrong_email@test.com" });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Verify2FA_UserNotFound_ReturnsBadRequest()
        {
            // Trips the user == null branch in Verify2FA
            var response = await _client.PostAsJsonAsync("/api/auth/verify-2fa", new { Email = "nobody@test.com", Code = "123456" });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Verify2FA_MissingParametersBeforeSetup_ReturnsBadRequest()
        {
            var uniqueEmail = $"2fa_missing_{Guid.NewGuid()}@test.com";
            var password = "Password123!";
            await _client.PostAsJsonAsync("/api/auth/register", new UserRegisterDto { FullName = "Missing Params", Email = uniqueEmail, Password = password });

            // Call verify without ever executing generate-2fa-secret first to trip missing parameters token branch
            var response = await _client.PostAsJsonAsync("/api/auth/verify-2fa", new { Email = uniqueEmail, Code = "123456" });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Verify2FA_InvalidCodeDuringSetup_ReturnsBadRequest()
        {
            var uniqueEmail = $"2fa_fail_{Guid.NewGuid()}@test.com";
            var password = "Password123!";
            await _client.PostAsJsonAsync("/api/auth/register", new UserRegisterDto { FullName = "2FA Fail", Email = uniqueEmail, Password = password });

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto { Email = uniqueEmail, Password = password });
            var authData = await loginResponse.Content.ReadFromJsonAsync<LoginResponseData>();

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authData!.Token);
            await _client.PostAsJsonAsync("/api/auth/generate-2fa-secret", new { Email = uniqueEmail });

            // Trip the validation failure path during an active workflow setup
            var verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-2fa", new { Email = uniqueEmail, Code = "000000" });
            Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
        }

        [Fact]
        public async Task Complete2FAWorkflow_ValidCode_ActivatesAndEnforces2FA()
        {
            // 1. Register and login to grab our bearer identity token
            var uniqueEmail = $"2fa_success_{Guid.NewGuid()}@test.com";
            var password = "Password123!";
            await _client.PostAsJsonAsync("/api/auth/register", new UserRegisterDto { FullName = "2FA Success", Email = uniqueEmail, Password = password });

            var loginResponse1 = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto { Email = uniqueEmail, Password = password });
            var authData1 = await loginResponse1.Content.ReadFromJsonAsync<LoginResponseData>();

            // 2. Execute the unverified secret key creation process
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authData1!.Token);
            var setupResponse = await _client.PostAsJsonAsync("/api/auth/generate-2fa-secret", new { Email = uniqueEmail });
            var setupData = await setupResponse.Content.ReadFromJsonAsync<Setup2FaResponse>();
            
            Assert.NotNull(setupData);
            Assert.NotEmpty(setupData.SecretKey);

            // 3. Generate a cryptographic real-time match using OtpNet
            var secretBytes = Base32Encoding.ToBytes(setupData.SecretKey);
            var totp = new Totp(secretBytes);
            var validCode = totp.ComputeTotp();

            // 4. Verify code to successfully activate 2FA (isInitialSetupWorkflow = true branch)
            var verifyResponse1 = await _client.PostAsJsonAsync("/api/auth/verify-2fa", new { Email = uniqueEmail, Code = validCode });
            verifyResponse1.EnsureSuccessStatusCode();
            var verifyData1 = await verifyResponse1.Content.ReadFromJsonAsync<LoginResponseData>();
            Assert.NotEmpty(verifyData1!.Token);

            // 5. Attempt a fresh login session — it must now detect 2FA activation and block
            var loginResponse2 = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto { Email = uniqueEmail, Password = password });
            var authData2 = await loginResponse2.Content.ReadFromJsonAsync<LoginResponseData>();
            Assert.True(authData2!.Requires2FA);

            // 6. Give it a broken code while 2FA is active to hit the final verification failure branch (isInitialSetupWorkflow = false branch)
            var invalidChallengeResponse = await _client.PostAsJsonAsync("/api/auth/verify-2fa", new { Email = uniqueEmail, Code = "000000" });
            Assert.Equal(HttpStatusCode.BadRequest, invalidChallengeResponse.StatusCode);

            // 7. Give it the true matching code to completely pass full authentication (isInitialSetupWorkflow = false branch)
            var validChallengeResponse = await _client.PostAsJsonAsync("/api/auth/verify-2fa", new { Email = uniqueEmail, Code = totp.ComputeTotp() });
            validChallengeResponse.EnsureSuccessStatusCode();
            var finalAuth = await validChallengeResponse.Content.ReadFromJsonAsync<LoginResponseData>();
            Assert.NotEmpty(finalAuth!.Token);
        }

        // ==================== REGISTRATION & INVITE CODE BRANCH TESTS ====================

        [Fact]
        public async Task Register_EmailAlreadyInUse_ReturnsBadRequest()
        {
            // Arrange
            var uniqueEmail = $"duplicate_{Guid.NewGuid()}@test.com";
            var registerDto = new UserRegisterDto { FullName = "First", Email = uniqueEmail, Password = "Password123!" };

            // Act 1: First registration succeeds
            await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Act 2: Second registration with the exact same email hits the userManager fallback branch
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithInvalidInviteCode_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new UserRegisterDto
            {
                FullName = "Invite Tester",
                Email = $"invite_fail_{Guid.NewGuid()}@test.com",
                Password = "Password123!",
                InviteCode = "INVALID-CODE-999" // Trips the invite == null branch
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            
            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Invalid or expired invite code", content);
        }

        [Fact]
        public async Task GenerateInviteCode_AsAuthorizedUser_ReturnsInviteCode()
        {
            // 1. Setup Company and get Owner Token
            var ownerEmail = $"owner_invite_{Guid.NewGuid()}@test.com";
            var password = "Password123!";
            await _client.PostAsJsonAsync("/api/auth/register-company", new UserRegisterDto { CompanyName = "Invite Corp", FullName = "Owner", Email = ownerEmail, Password = password });

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto { Email = ownerEmail, Password = password });
            var authData = await loginResponse.Content.ReadFromJsonAsync<LoginResponseData>();

            // 2. Authorize the client and hit the generation endpoint
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authData!.Token);
            
            // Act
            var inviteResponse = await _client.PostAsync("/api/auth/generate-invite", null);

            // Assert
            inviteResponse.EnsureSuccessStatusCode();
            var inviteData = await inviteResponse.Content.ReadFromJsonAsync<InviteResponseData>();

            Assert.NotNull(inviteData);
            Assert.False(string.IsNullOrEmpty(inviteData.InviteCode));
        }

        [Fact]
        public async Task Register_WithValidInviteCode_SucceedsAndConsumesInvite()
        {
            // 1. Setup Company and get Owner Token
            var ownerEmail = $"owner_valid_invite_{Guid.NewGuid()}@test.com";
            var password = "Password123!";
            await _client.PostAsJsonAsync("/api/auth/register-company", new UserRegisterDto { CompanyName = "Valid Invite Corp", FullName = "Owner", Email = ownerEmail, Password = password });

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto { Email = ownerEmail, Password = password });
            var authData = await loginResponse.Content.ReadFromJsonAsync<LoginResponseData>();

            // 2. Generate the real invite code
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authData!.Token);
            var inviteResponse = await _client.PostAsync("/api/auth/generate-invite", null);
            var inviteData = await inviteResponse.Content.ReadFromJsonAsync<InviteResponseData>();
            var validCode = inviteData!.InviteCode;

            // 3. Clear auth header so we are unauthenticated for standard registration
            _client.DefaultRequestHeaders.Authorization = null;

            // 4. Act: Register a new user using the valid code
            var workerEmail = $"worker_{Guid.NewGuid()}@test.com";
            var registerDto = new UserRegisterDto
            {
                FullName = "Worker User",
                Email = workerEmail,
                Password = password,
                InviteCode = validCode
            };
            
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            
            // Assert 1: First registration works
            registerResponse.EnsureSuccessStatusCode();

            // 5. Act: Try to reuse the consumed code
            var registerDto2 = new UserRegisterDto
            {
                FullName = "Thief User",
                Email = $"thief_{Guid.NewGuid()}@test.com",
                Password = password,
                InviteCode = validCode // Trips the invite.IsUsed == true branch
            };
            var reuseResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto2);
            
            // Assert 2: Second registration is blocked
            Assert.Equal(HttpStatusCode.BadRequest, reuseResponse.StatusCode);
        }
    }

    // Helper class to catch the JSON response from your Login endpoint
    public class LoginResponseData
    {
        public string Token { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public bool Requires2FA { get; set; }
    }

    // Helper class to catch the JSON response from the 2FA Setup endpoint
    public class Setup2FaResponse
    {
        public string SecretKey { get; set; } = string.Empty;
        public string AuthenticatorUri { get; set; } = string.Empty;
        public string SetupSessionId { get; set; } = string.Empty;
    }

    public class InviteResponseData
    {
        public string InviteCode { get; set; } = string.Empty;
        public DateTime ExpireAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}