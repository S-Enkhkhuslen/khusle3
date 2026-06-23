using UnityEngine;

public class Tank : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(new Vector3(15, 30, 45) * Time.deltaTime);
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            Controller.Instance.AddOxygen();
            Debug.Log("you add Your Oxygen");
            Destroy(gameObject);
        }
    }
}
