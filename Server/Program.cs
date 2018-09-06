using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Profile;

namespace Server
{
	class Program
	{
		const string _host = "127.0.0.1";
		const int _port = 50051;

		const string ServerCertChainPath = "../../../../_certificates/server1.pem";
		const string ServerPrivateKeyPath = "../../../../_certificates/server1.key";

		static void Main(string[] args)
		{
			ServerPort port = new ServerPort(_host, _port, CreateSslServerCredentials());

			Grpc.Core.Server server = new Grpc.Core.Server
			{
				Services = { Profile.Profile.BindService(new ProfileServiceImplementation()) },
				Ports = { port }
			};
			server.Start();

			Console.WriteLine("Profile server listening on port " + _port);
			Console.WriteLine("Press any key to stop the server...");
			Console.ReadKey();

			server.ShutdownAsync().Wait();
		}

		static SslServerCredentials CreateSslServerCredentials()
		{
			KeyCertificatePair keyCertPair = new KeyCertificatePair(
				File.ReadAllText(ServerCertChainPath),
				File.ReadAllText(ServerPrivateKeyPath));
			return new SslServerCredentials(new[] { keyCertPair });
		}
	}


	class ProfileServiceImplementation : Profile.Profile.ProfileBase
	{
		public override Task<ProfileResponse> GetProfile(ProfileRequest request, ServerCallContext context)
		{
			// Getting JWT from  metadata
			string jwt = context.RequestHeaders
				.FirstOrDefault(x => x.Key == "jwt")?.Value
				?? string.Empty;

			ShowRequest(request.Profile, jwt);

			ProfileResponse response = new ProfileResponse()
			{
				Profile = request.Profile,
				Name = $"Name {request.Profile}",
				Jwt = jwt
			};
			return Task.FromResult(response);
		}

		private void ShowRequest(string profile, string jwt)
		{
			Console.WriteLine();
			Console.WriteLine($"- Received request");
			Console.WriteLine($"  Body: Profile '{profile}'");
			Console.WriteLine($"  Metadata: JWT: '{jwt}'");
		}
	}
}
