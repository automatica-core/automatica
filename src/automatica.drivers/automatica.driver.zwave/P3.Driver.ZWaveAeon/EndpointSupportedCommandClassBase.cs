﻿using System;
using System.Linq;
using System.Threading.Tasks;
using P3.Driver.ZWaveAeon.Channel;
using P3.Driver.ZWaveAeon.Channel.Protocol;
using P3.Driver.ZWaveAeon.CommandClasses;

namespace P3.Driver.ZWaveAeon
{
    public abstract class EndpointSupportedCommandClassBase : CommandClassBase
    {
        private readonly byte _endpointId;

        protected EndpointSupportedCommandClassBase(Node node, CommandClass commandClass) :
            this(node, commandClass, 0)
        {
        }

        /// <summary>
        /// Use this constructor if this command class can be accessed only with endpoint id.
        /// </summary>
        /// <param name="node">The node</param>
        /// <param name="endpointId">The endpoint id. 0 means there is no endpoint.</param>
        protected EndpointSupportedCommandClassBase(Node node, CommandClass commandClass, byte endpointId)
            : base(node, commandClass)
        {
            _endpointId = endpointId;
        }

        protected async Task<byte[]> Send(Command command, Enum responseCommand)
        {
            if (_endpointId == 0)
            {
                // Send in regular manner.
                //
                return await Channel.Send(Node, command, responseCommand);
            }
            else
            {
                Command encapsolatedCommand = await EncapsulatCommandForEndpoint(command);
                byte[] response = await Channel.Send(Node, encapsolatedCommand, MultiChannel.command.Encap);
                return ExtractEndpointResponse(response, responseCommand);
            }
        }

        protected async Task Send(Command command)
        {
            if (_endpointId == 0)
            {
                // Send in regular manner.
                //
                await Channel.Send(Node, command);
            }
            else
            {
                Command encapsolatedCommand = await EncapsulatCommandForEndpoint(command);
                await Channel.Send(Node, encapsolatedCommand);
            }
        }

        private async Task<Command> EncapsulatCommandForEndpoint(Command command)
        {
            byte controllerId = await Node.Controller.GetNodeId();

            // Encapsulation have additional 4 params.
            const int encapsolationEdditionalParams = 4;
            byte[] payload = new byte[command.Payload.Length + encapsolationEdditionalParams];
            payload[0] = controllerId;
            payload[1] = _endpointId;
            payload[2] = command.ClassID;
            payload[3] = command.CommandID;
            for (int i = 0; i < command.Payload.Length; i++)
            {
                payload[i + encapsolationEdditionalParams] = command.Payload[i];
            }

            return new Command(CommandClass.MultiChannel, MultiChannel.command.Encap, payload);
        }

        private byte[] ExtractEndpointResponse(byte[] response, Enum expectedResponseCommand)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));
            if (response.Length < 4)
                throw new ReponseFormatException($"The response was not in the expected format. {GetType().Name}: Payload: {BitConverter.ToString(response)}");

            // Check sub report
            //
            if (response[0] != _endpointId)
                throw new ReponseFormatException($"Got response for endpoint id {response[0]}, while this command class serves endpoint {_endpointId}.");

            if (response[2] != Convert.ToByte(Class) || response[3] != Convert.ToByte(expectedResponseCommand))
            {
                throw new ReponseFormatException($"Got unexpected response for encapsolate message for command class {GetType().Name}. The response was for class {response[2]}, and was of type {response[3]}.");
            }

            return response.Skip(4).ToArray();
        }
    }
}
