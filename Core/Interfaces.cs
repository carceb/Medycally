using Medycally.Models;

namespace Medycally.Core
{
	public interface IClinicType
	{
		List<ClinicTypeModel> GetAll();
	}

	public interface IClinic
	{
		int AddOrEdit(ClinicModel model);
		List<ClinicModel> GetAll();
		List<ClinicModel> GetByUser(int securityUserId, bool isSuperAdmin, int? doctorId);
		List<ClinicModel> GetBySpecialtyId(int specialtyId);
		ClinicModel GetById(int clinicId);
		void Delete(int clinicId);
		List<ClinicDoctorModel> GetDoctors(int clinicId);
		void SaveDoctors(int clinicId, List<int> doctorIds);
	}

	public interface ISpecialty
	{
		int AddOrEdit(SpecialtyModel model);
		List<SpecialtyModel> GetAll();
		List<SpecialtyModel> GetActives();
		List<SpecialtyModel> GetByClinicId(int clinicId);
		void Delete(int specialtyId);
	}

	public interface IDoctorSchedule
	{
		int AddOrEdit(DoctorScheduleModel model);
		List<DoctorScheduleModel> GetByClinicIdAndSpecialtyId(int clinicId, int specialtyId);
		List<AdminScheduleModel> GetByClinicAndDoctor(int clinicId, int doctorId);
		int SaveSchedule(AdminScheduleModel model);
		void DeleteSchedule(int doctorScheduleId);
	}

	public interface IPatient
	{
		int AddOrEdit(PatientModel model);
		List<PatientModel> GetAll(string? search);
		PatientModel? GetById(int patientId);
		PatientModel? GetByIdNumber(int patientIdNumber);
		void Delete(int patientId);
		List<PatientFamilyMemberModel> GetFamily(int patientId);
		void LinkGuardian(int patientId, int guardianPatientId, int relationshipId);
		void RemoveGuardianLink(int patientId, int guardianPatientId);
	}

	public interface IReason
	{
		List<ReasonModel> GetAll(int specialtyId);
	}
	public interface IGeography
	{
		public List<GeographyModel> GetAllStates();
		public List<GeographyModel> GetStateById(int stateId);
		public List<GeographyModel> GetAllMunicipalities();
		public List<GeographyModel> GetMunicipalityByStateId(int stateId);
	}

	public interface IAppointment
	{
		int AddOrEdit(AppointmentModel model);
	}

	public interface IAppointmentQuery
	{
		List<DashboardAppointmentModel> GetByClinic(int clinicId, DateTime? date);
		List<AppointmentStatusModel> GetStatuses();
		void UpdateStatus(int appointmentId, int appointmentStatusId);
		AppointmentDetailModel? GetById(int appointmentId);
		void Delete(int appointmentId);
		void SetPatientId(int appointmentId, int patientId);
		List<CalendarAppointmentModel> GetForCalendar(int? doctorId, DateTime start, DateTime end);
	}

	public interface ICommonData
	{
		List<SexModel> GetAll();
		List<RelationshipModel> GetAllRelationship();
		List<StatusModel> GetAllStatuses();
	}

	public interface ISecurityUser
	{
		SecurityUserModel? Login(string email, string passwordHash);
		SecurityUserModel? GetByToken(string token);
		bool Activate(string token, string passwordHash);
		SecurityUserModel? ForgotPassword(string email);
		SecurityUserModel? GetByResetToken(string token);
		bool ResetPassword(string token, string passwordHash);
	}

	public interface ISecurityModule
	{
		List<NavigationModuleModel> GetUserPermissions(int securityUserId);
		List<string> GetAllActiveModuleUrls();
	}

	public interface IDoctor
	{
		int AddOrEdit(DoctorModel model);
		List<DoctorModel> GetAll();
		void Delete(int doctorId);
		List<SpecialtyDoctorModel> GetSpecialties(int doctorId);
		void SaveSpecialties(int doctorId, List<int> specialtyIds);
	}

	public interface IAdminUser
	{
		AdminUserModel AddOrEdit(AdminUserModel model);
		List<AdminUserModel> GetAll();
		void Delete(int securityUserId);
		List<SecurityRoleModel> GetAllRoles();
		string? ResendToken(int securityUserId);
		List<UserClinicModel> GetUserClinics(int securityUserId);
		void SaveUserClinics(int securityUserId, List<int> clinicIds);
	}

	public interface ISecurityRole
	{
		int AddOrEdit(SecurityRoleModel model);
		void Delete(int securityRoleId);
		List<SecurityRoleModuleModel> GetModules(int securityRoleId);
		void SaveModule(int securityRoleId, SecurityRoleModuleModel module);
	}

	public interface IAdminModule
	{
		List<SecurityModuleAdminModel> GetAll();
		int AddOrEdit(SecurityModuleAdminModel model);
		void Delete(int securityModuleId);
	}

	public interface IEmailService
	{
		Task SendActivationEmailAsync(string toEmail, string userName, string activationUrl);
		Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetUrl);
	}

	public interface IPatientHistory
	{
		PatientHistoryModel? GetByPatientId(int patientId);
		void Save(PatientHistoryModel model, int updatedByUserId);
	}

	public interface IMedicalAttention
	{
		List<QueueAppointmentModel> GetQueue(int clinicId, int? doctorId, DateTime? date);
		List<MedicalAttentionModel> GetHistoryByPatient(int patientIdNumber);
		List<MedicalAttentionModel> GetHistoryByGuardian(int guardianIdNumber);
		MedicalAttentionModel? GetByAppointment(int appointmentId);
		int Save(MedicalAttentionModel model);
		List<MedicalAttentionModel> GetAll();
	}

	public interface IExchangeRate
	{
		List<ExchangeRateModel> GetAll();
		void Save(string currencyCode, decimal rate);
	}

	public interface IClinicSpecialtyFee
	{
		List<ClinicSpecialtyFeeModel> GetByClinic(int clinicId);
		int Save(ClinicSpecialtyFeeModel model, int updatedByUserId);
	}
}
