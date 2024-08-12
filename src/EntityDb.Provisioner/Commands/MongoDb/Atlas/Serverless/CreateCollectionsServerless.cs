﻿using EntityDb.MongoDb.Extensions;
using MongoDB.Driver;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace EntityDb.Provisioner.Commands.MongoDb.Atlas.Serverless;

internal sealed class CreateCollectionsServerless : CommandBase
{
    private static async Task Execute(Arguments arguments)
    {
        const string expectedProtocol = "mongodb+srv://";

        var mongoDbAtlasClient = await GetMongoDbAtlasClient
        (
            arguments.GroupName,
            arguments.PublicKey,
            arguments.PrivateKey
        );

        var serverlessInstance = await mongoDbAtlasClient.GetServerlessInstance(arguments.InstanceName);

        if (serverlessInstance?.ConnectionStrings?.StandardSrv?.StartsWith(expectedProtocol) != true)
        {
            throw new InvalidOperationException();
        }

        var mongoClient =
            new MongoClient(
                $"{expectedProtocol}{arguments.ServiceName}:{arguments.ServicePassword}@{serverlessInstance.ConnectionStrings.StandardSrv[expectedProtocol.Length..]}/admin");

        await mongoClient.ProvisionSourceCollections(arguments.ServiceName);
    }

    private static void AddServerlessNameArgument(Command command)
    {
        var clusterNameArgument = new Argument<string>("instance-name")
        {
            Description = "The name of the Serverless Instance on which the database will be provisioned.",
        };

        command.AddArgument(clusterNameArgument);
    }

    public static void AddTo(Command parentCommand)
    {
        var createCollections = new Command("create-collections")
        {
            Handler = CommandHandler.Create<Arguments>(Execute),
        };

        AddMongoDbAtlasArguments(createCollections);
        AddServiceNameArgument(createCollections);
        AddServicePasswordArgument(createCollections);
        AddServerlessNameArgument(createCollections);

        parentCommand.AddCommand(createCollections);
    }

    public sealed record Arguments
    (
        string GroupName,
        string PublicKey,
        string PrivateKey,
        string ServiceName,
        string ServicePassword,
        string InstanceName
    );
}
