﻿using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using Microsoft.AspNet.WebHooks.Properties;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.WebHooks
{
	/// <summary>
	/// Provides an <see cref="IWebHookReceiver"/> implementation which supports WebHooks generated by Kudu. 
	/// Set the '<c>MS_WebHookReceiverSecret_Dropbox</c>' application setting to the application secrets, optionally using IDs
	/// to differentiate between multiple WebHooks, for example '<c>secret0, id1=secret1, id2=secret2</c>'.
	/// The corresponding WebHook URI is of the form '<c>https://&lt;host&gt;/api/webhooks/incoming/kudu/{id}</c>'.
	/// For details about Kudu WebHooks, see <c>https://github.com/projectkudu/kudu/wiki/Web-hooks</c>.
	/// </summary>
	public class KuduWebHookReceiver : WebHookReceiver
	{
		internal const string ReceiverName = "kudu";

		internal const string ActionParameter = "status";

		/// <inheritdoc />
		public override string Name
		{
			get { return ReceiverName; }
		}

		/// <inheritdoc />
		public override async Task<HttpResponseMessage> ReceiveAsync(string id, HttpRequestContext context, HttpRequestMessage request)
		{
			if (id == null)
			{
				throw new ArgumentNullException("id");
			}
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}

			if (request.Method != HttpMethod.Post)
			{
				return CreateBadMethodResponse(request);
			}

			// Read the request entity body
			JObject data = await ReadAsJsonAsync(request);

			// Get the action
			string action = data.Value<string>(ActionParameter);
			if (string.IsNullOrEmpty(action))
			{
				string msg = string.Format(CultureInfo.CurrentCulture, KuduReceiverResources.Receiver_BadBody, ActionParameter);
				context.Configuration.DependencyResolver.GetLogger().Error(msg);
				HttpResponseMessage badType = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
				return badType;
			}

			// Call registered handlers
			return await ExecuteWebHookAsync(id, context, request, new[] { action }, data);
		}
	}
}