using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Profile;

namespace Client
{
	class Program
	{
		const string _host = "127.0.0.1";
		const int _port = 50051;

		const string ClientCertAuthorityPath = "../../../../_certificates/ca.pem";

		static void Main(string[] args)
		{
			Channel channel = GetSslSecuredChanelWithAuthInterceptor();

			Profile.Profile.ProfileClient client = new Profile.Profile.ProfileClient(channel);

			Console.WriteLine("Client connected to server on port " + _port);
			Console.WriteLine("Press any key to stop the client...");

			for (int i = 0; i < 5; i++)
			{
				ProfileResponse response = client.GetProfile(
					new ProfileRequest() { Profile = (i + 1).ToString() });
				ShowResponse(response);
			}
			Console.ReadKey();
		}

		static Channel GetSslSecuredChanelWithAuthInterceptor()
		{
			// Interceptor that on each grpc call will be getting new jwt
			// and inserting it into request metadata
			AsyncAuthInterceptor asyncAuthInterceptor = new AsyncAuthInterceptor(async (context, metadata) =>
			{
				// make sure the operation is asynchronous.
				string jwt = await TaskRetreivingJwtFromHttpRequest();
				metadata.Add("jwt", jwt);
			});

			// SSL certificates are taken from grpc.io native project and
			// require special target host.
			List<ChannelOption> options = GetChanelOptions();

			// Using SSL credentials, because Insecure credentials won't allow
			// combining them with any CallCredentials
			ChannelCredentials cc = ChannelCredentials.Create(CreateSslCredentials(),
				CallCredentials.FromInterceptor(asyncAuthInterceptor));

			Channel channel = new Channel($"{_host}:{_port}", cc, options);
			return channel;
		}

		static async Task<string> TaskRetreivingJwtFromHttpRequest()
		{
			// Just some value different each time
			Task<string> task = Task.Run(() => $"bearer {DateTime.Now.Ticks.ToString()}");
			string jwt = await task;
			return jwt;

			// Method doesn't have to be static
		}
		static List<ChannelOption> GetChanelOptions()
		{
			return new List<ChannelOption>
			{
				new ChannelOption(ChannelOptions.SslTargetNameOverride, "foo.test.google.fr")
			};
		}
		static SslCredentials CreateSslCredentials()
		{
			// Using certificates from grpc.io test project
			return new SslCredentials(System.IO.File.ReadAllText(ClientCertAuthorityPath));
		}

		static void ShowResponse(ProfileResponse response)
		{
			Console.WriteLine();
			Console.WriteLine($"- Received response");
			Console.WriteLine($"  Body: Profile '{response.Profile}', " +
				$"Name '{response.Name}', JWT: '{response.Jwt}'");
		}
	}
}
