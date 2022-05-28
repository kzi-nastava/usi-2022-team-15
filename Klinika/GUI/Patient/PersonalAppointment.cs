﻿using Klinika.Models;
using Klinika.Repositories;
using Klinika.Roles;
using System.Data;
using Klinika.Services;
using Klinika.Utilities;

namespace Klinika.GUI.Patient
{
    public partial class PersonalAppointment : Form
    {
        private readonly PatientMain Parent;
        private Appointment? Appointment;
        private readonly bool IsDoctorSelected;

        #region Form
        public PersonalAppointment(PatientMain parent, Appointment appointment, bool isDoctorSelected = false)
        {
            InitializeComponent();
            Parent = parent;
            Appointment = appointment;
            IsDoctorSelected = isDoctorSelected;
        }
        private void LoadForm(object sender, EventArgs e)
        {
            Parent.Enabled = false;
            UIUtilities.FillDoctorComboBox(DoctorComboBox);
            FillFormDetails();
        }
        private void FillFormDetails()
        {
            if (Appointment == null || IsDoctorSelected)
            {
                SetupAsCreate();
                return;
            }
            SetupAsModify();
        }
        private void SetupAsCreate()
        {
            DoctorComboBox.Enabled = false;
            DoctorComboBox.SelectedIndex = Parent.DoctorComboBox.SelectedIndex;

            if (IsDoctorSelected)
            { 
                DoctorComboBox.SelectedIndex = DoctorComboBox.Items.IndexOf(UserRepository.GetInstance().Users.Where(x => x.ID == Appointment.DoctorID).FirstOrDefault());
                return; 
            }

            DatePicker.Enabled = false;
            DatePicker.Value = Parent.AppointmentDatePicker.Value;
        }
        private void SetupAsModify()
        {
            DatePicker.Enabled = true;
            DatePicker.Value = Appointment.DateTime.Date;
            TimePicker.Enabled = true;
            TimePicker.Value = Appointment.DateTime;
            DoctorComboBox.Enabled = true;
            SetDoctorComboBoxIndex();
        }
        private void ClosingForm(object sender, FormClosingEventArgs e)
        {
            Parent.Enabled = true;
        }
        #endregion

        #region Click functions
        private void ConfirmeButtonClick(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;

            if (Appointment == null || IsDoctorSelected) 
            {
                ConfirmeCreate();
                return;
            }
            ConfirmeModify();
        }
        private void ConfirmeCreate()
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to create this Appoinment?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                CreateInDatabase();
                Close();
            }
            return;
        }
        private void ConfirmeModify()
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to save the changes?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                ModifyInDatabase();
                Close();
            }
            return;
        }
        #endregion

        #region Helper functions
        private void CreateInDatabase()
        {
            Appointment = new Appointment(GetSelectedDoctorID(), Parent.Patient.ID, GetSelectedDateTime());

            AppointmentRepository.GetInstance().Create(Appointment);
            Parent.PersonalAppointmentsTable.Insert(Appointment);
            if (!IsDoctorSelected)
            {
                Parent.OccupiedAppointmentsTable.Insert(Appointment);
            }
        }
        private void ModifyInDatabase()
        {
            Appointment.DoctorID = GetSelectedDoctorID();
            Appointment.DateTime = GetSelectedDateTime();
            string description = PatientRequestService.GenerateDescription(GetSelectedDateTime(), GetSelectedDoctorID());

            bool needApproval = DateTime.Now.AddDays(2).Date >= Appointment.DateTime.Date;

            if (needApproval)
            {
                DialogResult sendConfirmation = MessageBox.Show("Changes that you have requested have to be check by secretary. " +
                    "Do you want to send request? ", "Send Request", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (sendConfirmation == DialogResult.Yes)
                {
                    PatientRequestService.SendModify(!needApproval, Appointment, description);
                }
                return;
            }

            PatientRequestService.SendModify(!needApproval, Appointment, description);
            AppointmentRepository.GetInstance().Modify(Appointment);
            Parent.PersonalAppointmentsTable.ModifySelected(Appointment);
        }  
        private int GetSelectedDoctorID()
        {
            return (DoctorComboBox.SelectedItem as User).ID;
        }
        private DateTime GetSelectedDateTime()
        {
            var selectedDate = DatePicker.Value;
            var selectedTime = TimePicker.Value;
            var dateTime = DateTime.Parse($"{selectedDate.Year}-{selectedDate.Month}-{selectedDate.Day} {selectedTime.Hour}:{selectedTime.Minute}");
            return dateTime;
        }
        private void SetDoctorComboBoxIndex()
        {
            User selected = UserRepository.GetDoctor(Appointment.DoctorID);
            DoctorComboBox.SelectedIndex = DoctorComboBox.Items.IndexOf(selected);
        }
        private bool ValidateForm()
        {
            if (!Parent.IsDateValid(GetSelectedDateTime())) return false;

            if (DoctorService.IsOccupied(GetSelectedDateTime(), GetSelectedDoctorID()))
            {
                MessageBox.Show("This time is occupied!", "Denied!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            return true;
        }
        #endregion
    }
}
