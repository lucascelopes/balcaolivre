import {
  agendaAndroidErrorResponse,
  agendaAndroidOptionsResponse,
  preRegisterAgendaAndroid,
} from "../../../../lib/agenda-android-server";

export async function POST(request: Request) {
  try {
    return await preRegisterAgendaAndroid(request);
  } catch (error) {
    return agendaAndroidErrorResponse(error);
  }
}

export async function OPTIONS() {
  return agendaAndroidOptionsResponse("POST, OPTIONS");
}
