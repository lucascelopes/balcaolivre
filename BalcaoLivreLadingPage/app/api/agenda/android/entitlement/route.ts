import {
  agendaAndroidErrorResponse,
  agendaAndroidOptionsResponse,
  getAgendaAndroidEntitlement,
} from "../../../../lib/agenda-android-server";

export async function GET(request: Request) {
  try {
    return await getAgendaAndroidEntitlement(request);
  } catch (error) {
    return agendaAndroidErrorResponse(error);
  }
}

export async function OPTIONS() {
  return agendaAndroidOptionsResponse("GET, OPTIONS");
}
