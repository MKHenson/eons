using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine;

public class Player : MonoBehaviour {
  public PlanetRenderer planet;
  private Rigidbody controller;

  // Start is called before the first frame update
  void Start() {
    planet.OnFirstChunksLoaded += onLoaded;
    controller = gameObject.GetComponent<Rigidbody>();
    controller.useGravity = false;
  }

  // Update is called once per frame
  void Update() {

  }

  void onLoaded() {
    planet.OnFirstChunksLoaded -= onLoaded;
    controller.useGravity = true;
  }
}
