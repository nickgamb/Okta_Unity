using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using Okta.Auth.Sdk;
using Okta.Sdk.Abstractions.Configuration;

public class PanelManager : MonoBehaviour {

	public Animator initiallyOpen;
	public Animator authMenu;
	public static bool isAuthenticated;

	public InputField Username;
	public InputField Password;
	public string OktaDomain;

	private int m_OpenParameterId;
	private Animator m_Open;
	private GameObject m_PreviouslySelected;

	const string k_OpenTransitionName = "Open";
	const string k_ClosedStateName = "Closed";

	public void OnEnable()
	{
		m_OpenParameterId = Animator.StringToHash (k_OpenTransitionName);

		if (initiallyOpen == null)
			return;

		OpenPanel(initiallyOpen);
	}

	public void OpenPanel (Animator anim)
	{
		if (!isAuthenticated)
        {
			anim = authMenu;
        }

		if (m_Open == anim)
			return;

		anim.gameObject.SetActive(true);
		var newPreviouslySelected = EventSystem.current.currentSelectedGameObject;

		anim.transform.SetAsLastSibling();

		CloseCurrent();

		m_PreviouslySelected = newPreviouslySelected;

		m_Open = anim;
		m_Open.SetBool(m_OpenParameterId, true);

		GameObject go = FindFirstEnabledSelectable(anim.gameObject);

		SetSelected(go);
	}

	static GameObject FindFirstEnabledSelectable (GameObject gameObject)
	{
		GameObject go = null;
		var selectables = gameObject.GetComponentsInChildren<Selectable> (true);
		foreach (var selectable in selectables) {
			if (selectable.IsActive () && selectable.IsInteractable ()) {
				go = selectable.gameObject;
				break;
			}
		}
		return go;
	}

	public void CloseCurrent()
	{
		if (m_Open == null)
			return;

		m_Open.SetBool(m_OpenParameterId, false);
		SetSelected(m_PreviouslySelected);
		StartCoroutine(DisablePanelDeleyed(m_Open));
		m_Open = null;
	}

	public async void Login()
	{
		var client = new AuthenticationClient(new OktaClientConfiguration
		{
			OktaDomain = OktaDomain,
		});

		var authnOptions = new AuthenticateOptions()
		{
			Username = Username.text.ToString(),
			Password = Password.text.ToString(),
		};

        try
        {
			var authnResponse = await client.AuthenticateAsync(authnOptions);

			Debug.Log("Authentication Status: " + authnResponse.AuthenticationStatus);
			if (authnResponse.AuthenticationStatus == "SUCCESS")
			{
				//Store the token
				Debug.Log(authnResponse.SessionToken);
				isAuthenticated = true;

				OpenPanel(initiallyOpen);
			}
			else
			{
				//Handle Errors
				Debug.Log("Authentication Failed...");
			}
		}
        catch (System.Exception ex)
        {
			Debug.Log(ex.Message);
        }
		
	}

	IEnumerator DisablePanelDeleyed(Animator anim)
	{
		bool closedStateReached = false;
		bool wantToClose = true;
		while (!closedStateReached && wantToClose)
		{
			if (!anim.IsInTransition(0))
				closedStateReached = anim.GetCurrentAnimatorStateInfo(0).IsName(k_ClosedStateName);

			wantToClose = !anim.GetBool(m_OpenParameterId);

			yield return new WaitForEndOfFrame();
		}

		if (wantToClose)
			anim.gameObject.SetActive(false);
	}

	private void SetSelected(GameObject go)
	{
		EventSystem.current.SetSelectedGameObject(go);
	}
}
