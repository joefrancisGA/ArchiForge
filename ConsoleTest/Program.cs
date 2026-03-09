using System;
using System.Text.Json;
using System.Text.Json.Nodes;


//archiforge new < projectName > == Creates a project skeleton + config.
// Obvious first step
// DONE

//archiforge run == Executes the v1 pipeline using the current directory project config.

//archiforge status <runId> == Shows current step + last error + artifact links.
// Informational

//archiforge artifacts <runId> == Lists produced artifacts with URIs (and optionally pulls them locally).
// Informational

//archiforge dev up == Starts local dependencies (Azurite, SQL Edge, Redis) + plugin containers via docker compose.
// Unclear - something to do with Docker
// Azurite is an Azure storage emulator, SQL Edge is a lightweight version of SQL Server for development,  
//   and Redis is an in-memory data structure store. Docker Compose is a tool for defining and running multi-container Docker applications.
//   This command likely sets up the necessary environment for development by starting these services in Docker containers.


namespace ArchiForge
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            ArchiForge_Run();

            if (args.Length == 0)
            {
                Console.WriteLine("Please provide a command. Available commands: new, dev up, run, status <runId>, artifacts <runId>");
                return;
            }

            string command = args[0];

            switch (command)
            {
                case "new":

                    if (args.Length <= 1) return;

                    string projectName = args[1];
                    ArchiForge_New(projectName);

                    return;

                case "dev":

                    if (args.Length > 1 && args[1] == "up")
                    {
                        ArchiForge_Dev_Up();
                        return;
                    }

                    Console.WriteLine("Expected archiforge dev up");
                    return;


                case "run":
                    ArchiForge_Run();
                    return;

                case "status":

                    if (args.Length <= 1) return;

                    string runId = args[1];
                    ArchiForge_Status(Convert.ToInt32(runId));

                    return;

                case "artifacts":

                    if (args.Length <= 1) return;

                    runId = args[1];
                    ArchiForge_Artifacts(Convert.ToInt32(runId));

                    return;

                default:

                    Console.WriteLine($"Unknown command: {command}");
                    return;
            }
        }


        // Appears to be working correctly, but needs more testing

        private static void ArchiForge_New(string projectName)
        {
            Console.WriteLine("Creating ArchiForge project " + projectName);

            ArchiForgeProjectScaffolder.ScaffoldOptions scaffoldOptions = new()
            {
                ProjectName = projectName,
                BaseDirectory = null, // current directory        
                OverwriteExistingFiles = true, // safe by default
                IncludeTerraformStubs = true
            };


            ArchiForgeProjectScaffolder.CreateProject(scaffoldOptions);
        }

        private static void ArchiForge_Dev_Up()
        {
        }


        // Reads archiforge.json, identifies plugins to run and their order, then executes them with the appropriate context (e.g. passing outputs from one as inputs to the next).
        /* What archiforge run actually does(step-by-step)
        Inputs
        •	archiforge.json
        •	inputs/brief.md
        •	environment variables for connection strings(dev)
        State transitions(keep these explicit)
            1.	Create Run row in SQL: status Pending
            2.	Status Running
            3.	For each pipeline step:
                o Resolve inputs(file path or prior step output reference)
                o Call plugin POST /execute
                o   Persist output artifacts to Blob
                o   Write Artifacts rows in SQL
                o   Write RunEvents(optional but recommended)
            4.	Status Succeeded(or Failed)
            5.	Write run-summary.json to outputs/ with artifact URIs
        */

/*        {
  "schema_version": "1.0.0",
  "artifact": {
    "type": "bundle",
    "name": "MA-LTSS Subscription Inventory + Diagrams",
    "media_type": "application/zip",
    "size_bytes": 4829137,
    "labels": [
      "azure",
      "inventory",
      "documentation",
      "ltss",
      "sgs"
    ]
    },
  "storage": {
    "primary": "https://storage.example.org/artifacts/sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef/uhg-ltss-2026-02-13.zip",
    "alternates": [
      {
        "uri": "https://mirror1.example.org/artifacts/sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef/uhg-ltss-2026-02-13.zip",
        "role": "mirror"
      },
      {
    "uri": "https://cache.example.org/tmp/uhg-ltss-2026-02-13.zip?sig=abc123",
        "role": "signed",
        "expires_at": "2026-02-20T17:00:00Z"
      },
      {
    "uri": "https://archive.example.org/artifacts/sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef/uhg-ltss-2026-02-13.zip",
        "role": "archive"
      }
    ]
  },
  "links": {
    "project": "https://repo.example.org/projects/uhg/ma-ltss",
    "version": "build-2026.02.13.001",
    "related": [
      {
        "rel": "source",
        "target": "https://repo.example.org/projects/uhg/ma-ltss/commit/abcde12345"
      },
      {
        "rel": "derived-from",
        "target": "https://storage.example.org/manifests/sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.json"
      },
      {
        "rel": "includes",
        "target": "https://storage.example.org/manifests/sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.json"
      }
    ]
  },
  "provenance": {
    "run": {
        "run_id": "run-20260213-001",
      "started_at": "2026-02-13T16:55:00Z",
      "ended_at": "2026-02-13T17:02:12Z",
      "actor": "joe.francis@uhg.example",
      "environment": "prod"
    },
    "prompt": {
        "prompt_ref": "https://storage.example.org/prompts/run-20260213-001.txt",
      "prompt_hash": {
            "alg": "sha256",
        "value": "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"
      }
    },
    "plugin": {
        "name": "archiforge.manifest",
      "version": "2.3.1",
      "ref": "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
      "entrypoint": "emit-manifest"
    }
},
  "integrity": {
    "hashes": [
      {
        "alg": "sha256",
        "value": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
      },
      {
        "alg": "sha512",
        "value": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
      },
      {
        "alg": "blake3",
        "value": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
      }
    ],
    "verification": "hash+signature",
    "signature": {
        "alg": "ed25519",
      "value": "ZWR1MjU1MTktc2lnbmF0dXJlLWJhc2U2NC1leGFtcGxl",
      "key_id": "kms://example/keyrings/uhg/keys/manifest-signing-key#1"
    }
},
  "immutability": {
    "mode": "content-addressed",
    "rules": [
      "forbid-overwrite",
      "require-hash-match",
      "require-signature",
      "require-content-addressed-uri"
    ]
  },
  "extensions": {
    "uhg": {
        "tenant": "SGS",
      "subscription_id": "00000000-0000-0000-0000-000000000000",
      "data_classification": "PHI",
      "arb": {
            "review_id": "ARB-2026-0213-07",
        "status": "draft"
      }
    },
    "notes": "Example manifest demonstrating maximum field coverage for validation testing."
  }
}*/


        private static void ArchiForge_Run()
        {
            // 1️⃣ Start with an empty root object
            JsonObject root = new JsonObject();

            // 2️⃣ Add schema_version
            root["schema_version"] = "1.0.0";

            // 3️⃣ Add artifact object
            var artifact = new JsonObject
            {
                ["type"] = "bundle",
                ["name"] = "MA-LTSS Subscription Inventory + Diagrams",
                ["media_type"] = "application/zip",
                ["size_bytes"] = 4829137
            };

            var labels = new JsonArray
            {
                "azure",
                "inventory",
                "documentation",
                "ltss",
                "sgs"
            };

            artifact["labels"] = labels;

            root["artifact"] = artifact;

            // 4️⃣ Add storage section
            var storage = new JsonObject
            {
                ["primary"] = "https://storage.example.org/artifacts/sha256:0123456789abcdef/uhg-ltss.zip"
            };

            var alternates = new JsonArray
            {
                new JsonObject
                {
                    ["uri"] = "https://mirror1.example.org/uhg-ltss.zip",
                    ["role"] = "mirror"
                },
                new JsonObject
                {
                    ["uri"] = "https://cache.example.org/uhg-ltss.zip?sig=abc123",
                    ["role"] = "signed",
                    ["expires_at"] = "2026-02-20T17:00:00Z"
                }
            };

            storage["alternates"] = alternates;
            root["storage"] = storage;

            // 5️⃣ Add integrity section
            var integrity = new JsonObject();

            var hashes = new JsonArray
            {
                new JsonObject
                {
                    ["alg"] = "sha256",
                    ["value"] = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
                },
                new JsonObject
                {
                    ["alg"] = "sha512",
                    ["value"] = "0123456789abcdef..."
                }
            };

            integrity["hashes"] = hashes;
            integrity["verification"] = "hash+signature";

            integrity["signature"] = new JsonObject
            {
                ["alg"] = "ed25519",
                ["value"] = "base64signatureexample",
                ["key_id"] = "kms://example/keyrings/uhg/keys/manifest-signing-key#1"
            };

            root["integrity"] = integrity;

            // 6️⃣ Add immutability
            var immutability = new JsonObject
            {
                ["mode"] = "content-addressed"
            };

            var rules = new JsonArray
            {
                "forbid-overwrite",
                "require-hash-match",
                "require-signature",
                "require-content-addressed-uri"
            };

            immutability["rules"] = rules;

            root["immutability"] = immutability;

            // 7️⃣ Add extensions
            root["extensions"] = new JsonObject
            {
                ["notes"] = "Example manifest demonstrating incremental construction."
            };

            // 8️⃣ Convert to JsonDocument (optional)
            JsonDocument doc = JsonDocument.Parse(root.ToJsonString());

            // 9️⃣ Dump to console (pretty)
            var options = new JsonSerializerOptions { WriteIndented = true };
            Console.WriteLine(JsonSerializer.Serialize(doc.RootElement, options));
        }

        private static void ArchiForge_Status(int runId)
        {
        }

        /*{
            "schema_version": "1.0.0",
            "artifact": {
                "type": "doc",
                "name": "LandingZone Blueprint",
                "media_type": "application/pdf",
                "size_bytes": 2480193,
                "labels": ["bclc", "lz", "blueprint"]
            },
            "storage": {
                "primary": "https://storage.example.org/artifacts/lz-blueprint.pdf",
                "alternates": [
                { "uri": "s3://my-bucket/artifacts/lz-blueprint.pdf", "role": "mirror" }
                ]
            },
            "links": {
                "project": "https://dev.azure.com/org/project/_wiki/wikis/project.wiki/1234",
                "version": "2026.02.07.1",
                "related": [
                {
                    "rel": "derived-from",
                    "target": "https://storage.example.org/manifests/source.manifest.json"
                }
                ]
            },
            "provenance": {
                "run": {
                    "run_id": "run-20260207-130701Z-8f2c",
                    "started_at": "2026-02-07T13:07:01-05:00",
                    "actor": "svc-archiforge",
                    "environment": "prod"
                },
                "prompt": {
                    "prompt_ref": "https://storage.example.org/prompts/prompt-8f2c.txt",
                    "prompt_hash": { "alg": "sha256", "value": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" }
                },
                "plugin": {
                    "name": "archiforge.manifest",
                    "version": "1.4.3",
                    "ref": "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
                }
            },
            "integrity": {
                "hashes": [
                { "alg": "sha256", "value": "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc" }
                ],
                "verification": "hash"
            },
            "immutability": {
                "mode": "immutable",
                "rules": ["forbid-overwrite", "require-hash-match"]
            }
        }*/

        private static void ArchiForge_Artifacts(int runId)
        {
        }
    }
}

