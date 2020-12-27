using System;

namespace DeltaVDesigner.Models
{
	public sealed class ComponentViewModel : ViewModelBase
	{
		public ComponentViewModel(Action<ComponentViewModel> deleteCallback)
		{
			m_deleteCallback = deleteCallback;
		}

		public string Id
		{
			get => VerifyAccess(m_id);
			set => SetPropertyField(value, ref m_id);
		}

		public decimal X
		{
			get => VerifyAccess(m_x);
			set => SetPropertyField(value, ref m_x);
		}

		public decimal Y
		{
			get => VerifyAccess(m_y);
			set => SetPropertyField(value, ref m_y);
		}

		public decimal Width
		{
			get => VerifyAccess(m_width);
			set => SetPropertyField(value, ref m_width);
		}

		public decimal Length
		{
			get => VerifyAccess(m_length);
			set => SetPropertyField(value, ref m_length);
		}

		public decimal Height
		{
			get => VerifyAccess(m_height);
			set => SetPropertyField(value, ref m_height);
		}

		public Dimensions GetDimensions() => new Dimensions(Width, Length, Height);

		public void Delete() => m_deleteCallback(this);

		readonly Action<ComponentViewModel> m_deleteCallback;

		string m_id;
		decimal m_x;
		decimal m_y;
		decimal m_width;
		decimal m_length;
		decimal m_height;
	}
}
