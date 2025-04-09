using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
class Program
{
    // 2 eksenli robotlar için motor durumlarını tutan Dictionary (2 robot)
    private static readonly Dictionary<string, Dictionary<string, bool>> motorStates = new()
    {
        {"robot1", new Dictionary<string, bool>
            {
                {"robot1_motor1", false},
                {"robot1_motor2", false}
            }
        },
        {"robot2", new Dictionary<string, bool>
            {
                {"robot2_motor1", false},
                {"robot2_motor2", false}
            }
        },
        // 6 eksenli robot için motorlar
        {"robot3", new Dictionary<string, bool>
            {
                {"robot3_motor1", false},
                {"robot3_motor2", false},
                {"robot3_motor3", false},
                {"robot3_motor4", false},
                {"robot3_motor5", false},
                {"robot3_motor6", false}
            }
        }
    };
    public static readonly Dictionary<string, Dictionary<string, float>> jointStates = new()
    {
        {"robot1", new Dictionary<string,float>
            {
                {"robot1_joint1", 0.0f},
                {"robot1_joint2", 0.0f}
            }
        },
        {"robot2", new Dictionary<string, float>
            {
                {"robot2_joint1", 0.0f},
                {"robot2_joint2", 0.0f}
            }
        },
        // 6 eksenli robot için mojointlar
        {"robot3", new Dictionary<string, float>
            {
                {"robot3_joint1", 0.0f},
                {"robot3_joint2", 0.0f},
                {"robot3_joint3", 0.0f},
                {"robot3_joint4", 0.0f},
                {"robot3_joint5", 0.0f},
                {"robot3_joint6", 0.0f}
            }
        }
    };
    private static string selectedRobotType = ""; // Seçilen robot tipi (2 eksenli ya da 6 eksenli)
    private static string robotName = ""; // Motor ismi için kullanılacak değişken
    private static string programCode = ""; // Program kodu için kullanılacak değişken
    static void Main()
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8000/api/robot/");
        listener.Start();
        Console.WriteLine("HTTP Server started on http://localhost:8000/api/robot/");
        HandleRequests(listener);
    }

    static void HandleRequests(HttpListener listener)
    {
        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            Console.WriteLine($"Request received: {context.Request.HttpMethod} {context.Request.Url}");
            ProcessRequest(context);
        }
    }

    static void ProcessRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;
        string urlPath = request.Url.AbsolutePath.Trim('/');
        // Console.WriteLine($"Processing request for: {urlPath}");
        string responseString = "";

        // Console.WriteLine($"Request method: {request.HttpMethod}");

        response.AddHeader("Access-Control-Allow-Origin", "*");
        response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

        // Handle preflight OPTIONS request for CORS
        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 204; // 200 yerine 204 daha yaygın
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
            response.ContentLength64 = 0;
            response.OutputStream.Close();
            return;
        }

        if (request.HttpMethod == "POST")
        {
            Console.WriteLine($"Request method: {urlPath}");
            // Robot tipi seçimi (2 eksenli veya 6 eksenli)
            if (urlPath == "api/robot/choose_robot_axis")
            {
                // Read the request body
                string requestBody = new StreamReader(request.InputStream).ReadToEnd();

                // Deserialize the incoming JSON data
                var robotSelection = JsonSerializer.Deserialize<RobotSelectionRequest>(requestBody);

                if (robotSelection != null)
                {
                    Console.WriteLine($"Axis count received: {robotSelection.type}");

                    // Validate and set the robot type
                    if (robotSelection.type == "scara" || robotSelection.type == "industrial")
                    {
                        selectedRobotType = robotSelection.type;
                        Console.WriteLine(selectedRobotType);
                        responseString = JsonSerializer.Serialize(new { message = $"Robot type set to {selectedRobotType}-axis" });
                        // Console.WriteLine($"Robot type set to {selectedRobotType}-axis");
                    }
                    else
                    {
                        responseString = JsonSerializer.Serialize(new { message = "Invalid axis count. Please select 'scara' or 'industrial'." });
                    }
                }
                else
                {
                    responseString = JsonSerializer.Serialize(new { message = "Invalid request body." });
                }
            }
            else if (urlPath == "api/robot/choose-active-robot")
            {
                //Burqada frontendeki robot seçimi yapılacak ve robotName değişkenine atanacak
                string requestBody = new StreamReader(request.InputStream).ReadToEnd();
                Console.WriteLine($"Request body: {requestBody}");
                var robotSelection = JsonSerializer.Deserialize<RobotSelectionRequest>(requestBody);
                if (robotSelection != null)
                {
                    Console.WriteLine($"Robot name received: {robotSelection.type}");
                    robotName = robotSelection.type;
                    responseString = JsonSerializer.Serialize(new { message = $"Robot name set to {robotName}" });
                }
                else
                {
                    responseString = JsonSerializer.Serialize(new { message = "Invalid request body." });
                }

            }
            else if (urlPath.Contains("toggle_"))
            {
                if (string.IsNullOrEmpty(selectedRobotType))
                {
                    responseString = "{\"message\": \"No robot selected. Please select robot type first.\"}";
                }
                else
                {
                    string motorKey = urlPath.Replace("api/robot/toggle_", "");

                    if (IsMotorInSelectedRobot(motorKey))
                    {
                        // Console.WriteLine($"Motor key received: {motorKey}");
                        robotName = motorKey.Split('_')[0];
                        // Console.WriteLine($"Robot name: {robotName}");
                        motorStates[robotName][motorKey] = !motorStates[robotName][motorKey];
                        responseString = CreateMotorResponse(motorKey, robotName);
                        // Console.WriteLine($"Motor state toggled: {motorKey} is now {(motorStates[robotName][motorKey] ? "ON" : "OFF")}");
                    }
                    else
                    {
                        responseString = "{\"message\": \"Motor not available for selected robot type.\"}";
                    }
                }
            }
            else if (urlPath == "api/robot/toggle-all-motors-on")
            {
                // Console.WriteLine(robotName);
                // Console.WriteLine("asdasdasdasdasdsa");
                if (string.IsNullOrEmpty(robotName))
                {
                    responseString = "{\"message\": \"No robot selected. Please select robot type first.\"}";
                }
                else
                {
                    foreach (var motor in motorStates[robotName])
                    {
                        motorStates[robotName][motor.Key] = true;
                    }
                    responseString = "{\"message\": \"All motors turned ON.\"}";
                }
            }
            else if (urlPath == "api/robot/emergency-stop")
            {
                if (string.IsNullOrEmpty(robotName))
                {
                    responseString = "{\"message\": \"No robot selected. Please select robot type first.\"}";
                }
                else
                {
                    foreach (var motor in motorStates[robotName])
                    {
                        motorStates[robotName][motor.Key] = false;
                    }
                    responseString = GetRobotStatus();
                }

            }
            else if (urlPath == "api/robot/general-emergency-stop")
            {
                // 2 motoru olan bütün robotları durdur
                foreach (var robot in motorStates)
                {
                    foreach (var motor in robot.Value)
                    {
                        motorStates[robot.Key][motor.Key] = false;
                    }
                }

                responseString = "{\"message\": \"All robots stopped.\"}";

            }
            else if (urlPath == "api/robot/update-joint-value")
            {
                string requestBody = new StreamReader(request.InputStream).ReadToEnd();
                var jointSelection = JsonSerializer.Deserialize<JointSelectionRequest>(requestBody);
                if (jointSelection.value != null)
                {
                    Console.WriteLine($"Joint value received: {jointSelection.value}");
                    Console.WriteLine($"Joint index: {jointSelection.jointIndex}");
                    // KADIRE MESAJ JOINT VALUE -1 Mİ 0 MI + 1 Mİ onu belli ediyor
                    // KADIRE MESAJ JOINT INDEX  1. mi 2. mi 0 İSE 1.EKSEN 1 İSE 2.EKSEN
                    responseString = JsonSerializer.Serialize(new { message = $"Motor{jointSelection.value + 1} hız veriliyor" });
                }
                else
                {
                    responseString = JsonSerializer.Serialize(new { message = "Invalid request body." });
                }
            }
            else if(urlPath == "api/robot/send-program-code")
            {
                string requestBody = new StreamReader(request.InputStream).ReadToEnd();
                Console.WriteLine($"Request body: {requestBody}");
                var programCodeRequest = JsonSerializer.Deserialize<RobotProgramCodeRequest>(requestBody);
                if (programCodeRequest != null)
                {
                    programCode = programCodeRequest.programCode;
                    Console.WriteLine(programCode);
                    // SEND DATA TO ROBOT USING GUI 
                    responseString = JsonSerializer.Serialize(new { message = $"Program code set to {programCode}" });
                }
                else
                {
                    responseString = JsonSerializer.Serialize(new { message = "Invalid request body." });
                }
            }
        }
        else if (request.HttpMethod == "GET" && urlPath == "api/robot/get_motor_status")
        {   
            responseString = GetRobotStatus();
        }
        else if (request.HttpMethod == "GET" && urlPath == "api/robot/selected_robot")
        {
            responseString = $"{{\"selected_robot\": \"{selectedRobotType}\"}}";
        }
        else if (request.HttpMethod == "GET" && urlPath == "api/robot/get_joint_value")
        {
            //create respornse stirng accordşng to robotName
            if (string.IsNullOrEmpty(robotName))
            {
                responseString = "{\"message\": \"No robot selected. Please select robot type first.\"}";
            }
            else
            {
                var jointDict = new Dictionary<string, float>();
                foreach (var joint in jointStates[robotName])
                {
                    string jointKey = joint.Key;
                    if (jointKey.Contains("_"))
                    {
                        jointKey = jointKey.Split('_')[1];
                    }
                    jointDict[jointKey] = joint.Value;
                }
                var res = new { joints = jointDict };
                responseString = JsonSerializer.Serialize(res);
            }   
        }


        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    static bool IsMotorInSelectedRobot(string motorKey)
    {
        // Seçilen robot tipine göre geçerli motorları kontrol et
        if (selectedRobotType == "scara")
        {
            // Console.WriteLine($"Motor key: {motorKey}");
            // 2 eksenli robotlar sadece robot1 ve robot2'nin motorlarını içerir
            // Console.WriteLine(motorKey.StartsWith("robot1_") || motorKey.StartsWith("robot2_"));
            return motorKey.StartsWith("robot1_") || motorKey.StartsWith("robot2_");
        }
        else if (selectedRobotType == "industrial")
        {
            // 6 eksenli robot sadece robot3'ün motorlarını içerir
            return motorKey.StartsWith("robot3_");
        }
        return false;
    }

    static string CreateMotorResponse(string motorName, string robotName)
    {
        return $"{{\"Motor\": \"{motorName}\", \"Durum\": \"{(motorStates[robotName][motorName] ? "ON" : "OFF")}\"}}";
    }

static string GetRobotStatus()
{
    if (string.IsNullOrEmpty(robotName))
    {
        return "{\"message\": \"No robot selected. Please select robot type first.\"}";
    }
    else
    {

        var motorsDict = new Dictionary<string, bool>();
        
     
        foreach (var motor in motorStates[robotName])
        {

            string motorKey = motor.Key;
            if (motorKey.Contains("_"))
            {
                motorKey = motorKey.Split('_')[1];
            }
            
            motorsDict[motorKey] = motor.Value;
        }
        

        var response = new { motors = motorsDict };
        
        return JsonSerializer.Serialize(response);
    }
}
}
public class RobotSelectionRequest
{
    public string type { get; set; }
}
public class JointSelectionRequest
{
    public int jointIndex { get; set; }
    public double value { get; set; }
}

public class RobotProgramCodeRequest
{
    public string programCode { get; set; }
}