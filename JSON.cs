using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;

public class JSON : MonoBehaviour
{
    private const string apiKey = "f24c72f0921e27809861a179dace47a6";
    private string weatherUrl = "https://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}&appid={2}&units=metric";
    private string reverseGeocodingUrl = "https://api.openweathermap.org/geo/1.0/reverse?lat={0}&lon={1}&limit=1&appid={2}";

    [SerializeField]
    private GameObject luga; 

    private ARRaycastManager arRaycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private float latitude;
    private float longitude;

    void Start()
    {
        arRaycastManager = FindObjectOfType<ARRaycastManager>();
        StartCoroutine(GetLocation());
    }

    private IEnumerator GetLocation()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("Геолокация отключена на устройстве.");
            yield break;
        }

        Input.location.Start();

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1)
        {
            Debug.LogError("Время ожидания истекло.");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Не удалось определить местоположение.");
            yield break;
        }
        else
        {
            latitude = Input.location.lastData.latitude;
            longitude = Input.location.lastData.longitude;
            Debug.Log($"Координаты: Широта = {latitude}, Долгота = {longitude}");

            StartCoroutine(GetCityNameFromCoordinates(latitude, longitude));
        }

        Input.location.Stop();
    }

    private IEnumerator GetCityNameFromCoordinates(float lat, float lon)
    {
        string url = string.Format(reverseGeocodingUrl, lat, lon, apiKey);
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Ошибка при получении названия города: " + webRequest.error);
            }
            else
            {
                ProcessCityNameResponse(webRequest.downloadHandler.text);
            }
        }
    }

    private void ProcessCityNameResponse(string jsonResponse)
    {
        try
        {
            JArray geocodingData = JArray.Parse(jsonResponse);
            if (geocodingData.Count > 0)
            {
                string cityName = geocodingData[0]["name"]?.ToString();
                Debug.Log("Город: " + cityName);

                StartCoroutine(GetWeather(latitude, longitude));
            }
            else
            {
                Debug.LogError("Название города не найдено.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Ошибка при обработке данных: " + ex.Message);
        }
    }

    private IEnumerator GetWeather(float lat, float lon)
    {
        string url = string.Format(weatherUrl, lat, lon, apiKey);
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Ошибка: " + webRequest.error);
            }
            else
            {
                ProcessResponse(webRequest.downloadHandler.text);
            }
        }
    }

    private void ProcessResponse(string jsonResponse)
    {
        JObject weatherData = JObject.Parse(jsonResponse);

        var rain = weatherData["rain"]?["1h"]?.Value<float>() ?? 0;

        Debug.Log("Количество осадков за последний час: " + rain + " мм");

        CreateObjectsBasedOnRain(rain);
    }

    private void CreateObjectsBasedOnRain(float rainAmount)
    {
        int numberOfObjects = Mathf.FloorToInt(rainAmount);

        for (int i = 0; i < numberOfObjects; i++)
        {
            if (arRaycastManager.Raycast(new Vector2(Screen.width / 2, Screen.height / 2), hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                Instantiate(luga, hitPose.position, hitPose.rotation);
            }
        }
    }
}