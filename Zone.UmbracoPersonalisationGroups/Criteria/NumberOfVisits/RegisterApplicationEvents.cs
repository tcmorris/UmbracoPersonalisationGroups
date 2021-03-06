﻿namespace Zone.UmbracoPersonalisationGroups.Criteria.NumberOfVisits
{
    using System;
    using System.Web;
    using Helpers;
    using Umbraco.Core;
    using Umbraco.Core.Configuration;
    using Zone.UmbracoPersonalisationGroups.Configuration;

    /// <summary>
    /// Registered required Umbraco application events for the number of visits criteria - to track via a cookie
    /// the number of times the visitor has accessed the site
    /// </summary>
    /// <remarks>See: https://our.umbraco.org/Documentation/Reference/Events-v6/Application-Startup</remarks>
    public class RegisterApplicationEvents : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            if (CriteriaConfigHelpers.IsCriteriaInUse(NumberOfVisitsPersonalisationGroupCriteria.CriteriaAlias))
            {
                UmbracoApplicationBase.ApplicationInit += ApplicationInit;
            }
        }

        private void ApplicationInit(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            app.PostRequestHandlerExecute += TrackSession;
        }
        
        private static void TrackSession(object sender, EventArgs e)
        {
            var httpContext = HttpContext.Current;
            var config = UmbracoConfig.For.PersonalisationGroups();

            // Check if session cookie present
            var sessionCookie = httpContext.Request.Cookies[config.CookieKeyForTrackingIfSessionAlreadyTracked];
            if (sessionCookie == null)
            {
                // If not, create or update the number of visits cookie
                var trackingCookie = httpContext.Request.Cookies[config.CookieKeyForTrackingNumberOfVisits];
                if (trackingCookie != null)
                {
                    int cookieValue;
                    if (int.TryParse(trackingCookie.Value, out cookieValue))
                    {
                        trackingCookie.Value = (cookieValue + 1).ToString();
                    }
                    else
                    {
                        trackingCookie.Value = "1";
                    }
                }
                else
                {
                    trackingCookie = new HttpCookie(config.CookieKeyForTrackingNumberOfVisits)
                    {
                        Value = "1",
                        HttpOnly = true,
                    };
                }

                trackingCookie.Expires = DateTime.Now.AddDays(config.NumberOfVisitsTrackingCookieExpiryInDays);
                httpContext.Response.Cookies.Add(trackingCookie);

                // Set the session cookie so we don't keep updating on each request
                sessionCookie =
                    new HttpCookie(config.CookieKeyForTrackingIfSessionAlreadyTracked)
                    {
                        Value = "1",
                        HttpOnly = true,
                    };
                httpContext.Response.Cookies.Add(sessionCookie);
            }
        }
    }
}
