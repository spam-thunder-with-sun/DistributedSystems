﻿using Akka.Actor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET
{
    //Funzioni per eseguire l'opzione get e read
    internal partial class Node : UntypedActor
    {
        //Hashset thread-safe per le richieste get
        readonly ConcurrentDictionary<int, GetDataStructure> getRequestsData = new();

        protected void OnGet(GetMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received GET from {Sender.Path.Name} => Key:{message.Key}");

            //Genero un numero casuale id della richiesta GET
            int getID = SuperMain.mersenneTwister.Next();

            //Alloco lo spazio e salvo Key e nome del nodo che ha fatto la richiesta
            if (!getRequestsData.TryAdd(getID, new GetDataStructure(message.Key, Sender.Path.Name)))
                throw new Exception("Errors adding item to getRequestsData Dictionary");

            //Imposto un timer per inviare una risposta di timeout al client in caso di non raggiungimento del quorum
            System.Timers.Timer timoutTimer = new System.Timers.Timer(TIMEOUT_TIME);
            //La variabile Sender e Self non sono presenti nel contesto del Timer quindi salvo i riferimenti
            IActorRef SenderRef = Sender;
            IActorRef SelfRef = Self;
            //Funzione di callback
            timoutTimer.Elapsed += (source, e) =>
            {
                //Rimuovo i dati della richiesta GET se esistono
                if (getRequestsData.TryRemove(getID, out GetDataStructure? getRequestData))
                {
                    //Se i dati della richiesta GET ci sono ancora significa che non è stata soddisfatta quindi invio un timeout al client
                    //Invio la risposta di timeout
                    SenderRef.Tell(new GetResponseMessage(message.Key, true), SelfRef);

                    if (sendDebug)
                        lock (Console.Out)
                            Console.WriteLine($"{SelfRef.Path.Name} sended GET RESPONSE (TIMEOUT) to {SenderRef.Path.Name} => Key:{message.Key}");
                }
                else if (generalDebug)
                    lock (Console.Out)
                        Console.WriteLine($"{SelfRef.Path.Name} TIMEOUT not achieved => Key:{message.Key}");
                //Se getRequestData è null posso ignorare la risposta in quanto è già stata soddisfatta
            };
            timoutTimer.AutoReset = false;
            timoutTimer.Enabled = true;

            //Recupero i nodi che tengono quel valore
            List<uint> nodesWithValue = FindNodesThatKeepKey(message.Key);

            //Invio messaggi di READ a tutti gli altri nodi che hanno il valore
            foreach (uint node in nodesWithValue)
                Context.ActorSelection($"/user/node{node}").Tell(new ReadMessage(message.Key, getID));

            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} sended READ to ALL NODES => Key:{message.Key}");
        }

        protected void OnRead(ReadMessage message)
        {
            Document? fetchedData = data[message.Key];
            ReadResponseMessage responseMessage;
            //Se non ho l'oggetto della chiave, ritorno null
            if (fetchedData is not null)
                responseMessage = new(message.Key, fetchedData.Value, message.GetId, fetchedData.Version, fetchedData.PreWriteBlock);
            else
                responseMessage = new(message.Key, fetchedData?.Value, message.GetId);

            //Invio la risposta
            Sender.Tell(responseMessage, Self);
        }

        private void OnReadResponse(ReadResponseMessage message)
        {
            //Prendo i dati della richiesta GET se esistono
            if (getRequestsData.TryGetValue(message.GetId, out GetDataStructure? getRequestData))
            {
                //Aggiungo il valore ricevuto ai dati
                getRequestData.Add(message.Value, message.Version, message.PreWriteBlock);

                if (receiveDebug)
                    lock (Console.Out)
                        Console.WriteLine($"{Self.Path.Name} received READ RESPONSE from {Sender.Path.Name} => Key:{message.Key} Value:{message.Value ?? "null"} Version: {message.Version} PreWriteBlock: {message.PreWriteBlock}");

                //Cerco di prendere il valore da restituire alla GET
                string? returnValue = getRequestData.GetReturnValue();

                //GetReturnValue ritorna il valore più recente se ho abbastanza risposte i.e. >= READ_QUORUM
                //oppure torna null nel caso in cui:
                //- c'è una write in corso di cui non ho (ancora) il valore
                //- non ho risposte >= READ_QUORUM
                //Bisogna vedere se risco ad eliminare i dati della richiesta GET (i.e. potrebbe essere che il timeout sia partito nel frattempo)
                if (returnValue is not null && getRequestsData.TryRemove(message.GetId, out GetDataStructure? getRequestDataRemoved))
                {
                    //Invio la risposta al nodo
                    Context.ActorSelection($"/user/{getRequestDataRemoved.NodeName}").Tell(new GetResponseMessage(message.Key, returnValue));

                    if (sendDebug)
                        Console.WriteLine($"{Self.Path.Name} sended GET RESPONSE (QUORUM ACHIEVED) to {Sender.Path.Name} => Key:{message.Key} Value:{returnValue ?? "null"}");
                }
            }
            else if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} received READ RESPONSE (IGNORED) from {Sender.Path.Name} => Key:{message.Key} Value:{message.Value ?? "null"}");

            //Se getRequestData è null posso ignorare la risposta in quanto è già stata soddisfatta
        }
    }
}
