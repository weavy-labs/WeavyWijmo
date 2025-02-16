﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using WeavyWijmo.Data;
using WeavyWijmo.Models;

namespace WeavyWijmo.Controllers;

[ApiController]
[Route("[controller]")]
public class WeavyTokenController : ControllerBase
{
    ApplicationDbContext _ctx;

    public WeavyTokenController(ApplicationDbContext ctx)
    {
        _ctx = ctx;
    }

    [HttpGet]
    public string Get(string userId)
    {
        var user = _ctx.Users.Find(userId);
        if (user != null)
        {
            var theToken = GenerateToken(user);
            return theToken;
        }
        return "";
    }

    // generates a JWT token to use with Weavy
    private static string GenerateToken(ApplicationUser user)
    {
        // get first and last names from the email
        ParseEmail(user.Email, out string firstName, out string lastName);

        var secret = "dvG7gDigjzamH3g3DhwnyWFJbSZCSJC2"; // from https://get.weavy.io/
        var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        var descriptor = new SecurityTokenDescriptor
        {
            // see https://www.weavy.com/docs/frontend/authentication
            Subject = new ClaimsIdentity(new Claim[]
            {
                // required claims
                new Claim("iss", "get.weavy.io"), // Weavy Client Id
                new Claim("sub", user.Id), // unique user id

                // additional/optional claims
                new Claim("dir", "sandbox"),
                new Claim("email", user.Email),
                new Claim("family_name", lastName),
                new Claim("given_name", firstName),
                new Claim("name", firstName + " " + lastName),
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
        };
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        var tokenStr = handler.WriteToken(token);
        return tokenStr;
    }
    private static void ParseEmail(string eMail, out string firstName, out string lastName)
    {
        firstName = lastName = eMail;
        eMail = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(eMail);
        var rx = new Regex(@"([A-Za-z]+|@)\s*");
        var matches = rx.Matches(eMail);
        if (matches != null && matches.Count > 0)
        {
            foreach (Match m in matches)
            {
                if (m.Index == 0)
                {
                    firstName = m.Value;
                }
                if (m.Value == "@")
                {
                    break;
                }
                lastName = m.Value;
            }
        }
    }

    private static string GenerateToken(string fakeToken)
    {
        // get data from token
        var handler = new JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(fakeToken);
        var dct = jwtSecurityToken.Payload;

        // create a new token with the same data
        var secret = "dvG7gDigjzamH3g3DhwnyWFJbSZCSJC2"; // get.weavy.io
        //var secret = "Ux3Ko8vRGjGhfX34ENciHyagqSwbL5EM"; // sandbox
        var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim("iss", (string)dct["iss"]), // Weavy Client Id
                new Claim("dir", (string)dct["dir"]), // what's this?
                new Claim("sub", ((Int64)dct["sub"]).ToString(), ClaimValueTypes.Integer64), // unique user id
                new Claim("given_name", (string)dct["given_name"]),
                new Claim("family_name", (string)dct["family_name"]),
                new Claim("email", (string)dct["email"]),
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
        };
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

}
