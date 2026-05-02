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
	}

	public interface ISecurityModule
	{
		List<NavigationModuleModel> GetUserPermissions(int securityUserId);
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
		int AddOrEdit(AdminUserModel model);
		List<AdminUserModel> GetAll();
		void Delete(int securityUserId);
		List<SecurityRoleModel> GetAllRoles();
	}

	public interface IMedicalAttention
	{
		List<QueueAppointmentModel> GetQueue(int clinicId, int? doctorId, DateTime? date);
		List<MedicalAttentionModel> GetHistoryByPatient(int patientIdNumber);
		List<MedicalAttentionModel> GetHistoryByGuardian(int guardianIdNumber);
		MedicalAttentionModel? GetByAppointment(int appointmentId);
		int Save(MedicalAttentionModel model);
	}
}
